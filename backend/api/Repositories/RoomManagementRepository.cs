using api.DTOs;
using api.DTOs.Helpers;
using api.Enums;
using api.Extensions;
using api.Helpers;
using api.Interfaces;
using api.Models;
using api.Settings;
using Microsoft.AspNetCore.Identity;
using MongoDB.Bson;
using MongoDB.Driver;

namespace api.Repositories;

public class RoomManagementRepository : IRoomManagementRepository
{
    #region Variables and Constructor

    private readonly IMongoCollection<AppUser> _collectionUsers;
    private readonly IMongoCollection<Room> _collectionRooms;
    private readonly IUserRepository _userRepository;
    private readonly IRoomMembershipRepository _roomMembershipRepository;
    private readonly IRoomJoinRequestRepository _roomJoinRequestRepository;
    private readonly UserManager<AppUser> _userManager;
    private readonly IMongoClient _client;

    public RoomManagementRepository(IMongoClient client, IMyMongoDbSettings dbSettings,
        UserManager<AppUser> userManager,
        IUserRepository userRepository, IRoomMembershipRepository roomMembershipRepository,
        IRoomJoinRequestRepository roomJoinRequestRepository)
    {
        var database = client.GetDatabase(dbSettings.DatabaseName);
        _collectionUsers = database.GetCollection<AppUser>(AppVariablesExtensions.CollectionUsers);
        _collectionRooms = database.GetCollection<Room>(AppVariablesExtensions.CollectionRooms);
        database.GetCollection<RoomChat>(AppVariablesExtensions.CollectionRoomsChats);
        database.GetCollection<MembershipProposal>(AppVariablesExtensions.CollectionMembershipProposals);

        _client = client;
        _userManager = userManager;
        _userRepository = userRepository;
        _roomMembershipRepository = roomMembershipRepository;
        _roomJoinRequestRepository = roomJoinRequestRepository;
    }

    #endregion

    public async Task<OperationResult> CreateRoomAsync(CreateRoomDto request, ObjectId userId,
        CancellationToken cancellationToken)
    {
        string cleanRoomName = request.RoomName.ToNormalized();

        FindOptions options = new()
        {
            Collation = new Collation("en", strength: CollationStrength.Secondary)
        };

        Task<bool> isRoomExistsTask = _collectionRooms.Find(room => room.Name == cleanRoomName, options)
            .AnyAsync(cancellationToken);

        Task<AppUser> targetUserTask =
            _collectionUsers.Find(user => user.Id == userId).FirstOrDefaultAsync(cancellationToken);

        await Task.WhenAll(targetUserTask, isRoomExistsTask);

        AppUser? targetUser = await targetUserTask;
        bool isRoomExists = await isRoomExistsTask;

        if (targetUser is null)
        {
            return new(
                false,
                Error: new CustomError(
                    ErrorCode.UserNotFound,
                    "The given user does not exist."
                )
            );
        }

        if (isRoomExists)
        {
            return new(
                false,
                Error: new CustomError(
                    ErrorCode.RoomAlreadyExist,
                    "Room already exists."
                )
            );
        }

        if (!Enum.TryParse<RoomType>(request.RoomType.Trim(), true, out var roomType))
        {
            return new(
                false,
                Error: new CustomError(
                    ErrorCode.InvalidType,
                    "Invalid room type."
                )
            );
        }

        Room room = new()
        {
            Id = ObjectId.GenerateNewId(),
            Name = cleanRoomName,
            OwnerId = userId,
            RoomType = roomType,
            MemberIds = [userId],
            MembersCount = 1,
            CreatedAt = DateTime.UtcNow
        };

        using IClientSessionHandle session = await _client.StartSessionAsync(null, cancellationToken);

        session.StartTransaction();

        try
        {
            await _collectionRooms.InsertOneAsync(session, room, null, cancellationToken);

            bool hasOwnerRole = await _userManager.IsInRoleAsync(targetUser, "owner");
            if (!hasOwnerRole)
            {
                IdentityResult roleResult = await _userManager.AddToRoleAsync(targetUser, "owner");

                if (!roleResult.Succeeded)
                {
                    throw new Exception("Failed to add user to the owner role.");
                }
            }

            await session.CommitTransactionAsync(cancellationToken);
        }
        catch (Exception e)
        {
            await session.AbortTransactionAsync(cancellationToken);

            return new(
                false,
                Error: new CustomError(
                    ErrorCode.TransactionFailed,
                    $"Transaction failed. {e.Message}"
                )
            );
        }

        return new(
            true,
            null
        );
    }

    public async Task<OperationResult<PagedList<Room>>> GetAllAsync(RoomParams roomParams,
        CancellationToken cancellationToken)
    {
        PagedList<Room> rooms = await PagedList<Room>.CreatePagedListAsync(CreateRoomQuery(roomParams),
            roomParams.PageNumber,
            roomParams.PageSize, cancellationToken);

        return new(
            true,
            rooms,
            null
        );
    }

    public async Task<bool> CheckIsOwnRoomAsync(ObjectId userId, ObjectId roomId, CancellationToken cancellationToken)
        => await _collectionRooms.Find(doc => doc.Id == roomId && doc.OwnerId == userId).AnyAsync(cancellationToken);

    public async Task<OperationResult<RoomResponse>> GetRoomByIdAsync(ObjectId roomId, ObjectId userId,
        CancellationToken cancellationToken)
    {
        bool isUserExists = await _collectionUsers.Find(doc => doc.Id == userId).AnyAsync(cancellationToken);

        if (!isUserExists)
        {
            return new(
                false,
                Error: new(
                    ErrorCode.UserNotFound,
                    "The given user does not exist."
                )
            );
        }

        Room? targetRoom = await _collectionRooms.Find(doc => doc.Id == roomId).FirstOrDefaultAsync(cancellationToken);

        if (targetRoom is null)
        {
            return new(
                false,
                Error: new(
                    ErrorCode.RoomNotFound,
                    "There is no room associated with this request."
                )
            );
        }

        if (targetRoom.RoomType == RoomType.Private && !targetRoom.MemberIds.Contains(userId) &&
            targetRoom.OwnerId != userId)
        {
            return new(
                false,
                Error: new(
                    ErrorCode.NotRoomMember,
                    "You are not the member of this room. Please join this room first."
                )
            );
        }

        string userName = await _userRepository.GetUserNameByIdAsync(userId, cancellationToken);

        bool isMember =
            await _roomMembershipRepository.CheckIsMemberAsync(userId, targetRoom.Id!.Value, cancellationToken);
        bool hasPendingReq =
            await _roomJoinRequestRepository.CheckHasPendingRequestsAsync(userId, targetRoom.Id!.Value,
                cancellationToken);
        bool isOwner = await CheckIsOwnRoomAsync(userId, targetRoom.Id!.Value, cancellationToken);

        return new(
            true,
            Mappers.ConvertRoomToRoomResponse(targetRoom, userName, isMember, hasPendingReq, isOwner),
            null
        );
    }

    public async Task<OperationResult> UpdateRoomAsync(ObjectId roomId, ObjectId userId, RoomUpdateDto request,
        CancellationToken cancellationToken)
    {
        Room? targetRoom = await _collectionRooms.Find(doc => doc.Id == roomId).FirstOrDefaultAsync(cancellationToken);

        if (targetRoom is null)
        {
            return new(
                false,
                new(
                    ErrorCode.RoomNotFound,
                    "There is no room associated with this request."
                )
            );
        }

        if (targetRoom.OwnerId != userId)
        {
            return new(
                false,
                new(
                    ErrorCode.NotRoomOwner,
                    "You are not the owner of this room."
                )
            );
        }

        UpdateDefinition<Room> updateDef = Builders<Room>.Update
            .Set(doc => doc.Name, request.RoomName)
            .Set(doc => doc.RoomType, request.RoomType);

        UpdateResult updateResult =
            await _collectionRooms.UpdateOneAsync(doc => doc.Id == roomId, updateDef, null, cancellationToken);

        if (updateResult.ModifiedCount == 0)
        {
            return new(
                false,
                new(
                    ErrorCode.UpdateFailed,
                    "Update failed."
                )
            );
        }

        return new(
            true,
            null
        );
    }

    public async Task<OperationResult<IEnumerable<RoomResponse>>> GetRoomsCreatedByUserAsync(ObjectId userId,
        string userName,
        CancellationToken cancellationToken)
    {
        List<Room> rooms = await _collectionRooms
            .Find(doc => doc.OwnerId == userId).ToListAsync(cancellationToken);

        if (!rooms.Any())
        {
            return new(
                true,
                [],
                null
            );
        }

        IEnumerable<RoomResponse> roomResponses = rooms
            .Select(r => Mappers.ConvertRoomToRoomResponse(r, userName, true, false, true))
            .ToList();

        return new(
            true,
            roomResponses,
            null
        );
    }

    private IQueryable<Room> CreateRoomQuery(RoomParams roomParams)
    {
        IQueryable<Room> query = _collectionRooms.AsQueryable();

        query = query.OrderByDescending(doc => doc.MembersCount).ThenBy(doc => doc.Id);

        query = roomParams.OrderBy switch
        {
            OrderByEnum.Public => query.Where(doc => doc.RoomType == RoomType.Public),
            OrderByEnum.Private => query.Where(doc => doc.RoomType == RoomType.Private),
            _ => query
        };

        if (!string.IsNullOrEmpty(roomParams.Search))
        {
            roomParams.Search = roomParams.Search.ToUpper();

            query = query.Where(
                doc => doc.Name.Contains(roomParams.Search, StringComparison.CurrentCultureIgnoreCase));
        }

        return query;
    }
}