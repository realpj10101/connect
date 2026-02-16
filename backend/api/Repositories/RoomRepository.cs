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

public class RoomRepository : IRoomRepository
{
    #region Variables and Constructor

    private readonly IMongoCollection<AppUser> _collectionUsers;
    private readonly IMongoCollection<Room> _collectionRooms;
    private readonly UserManager<AppUser> _userManager;
    private readonly IMongoClient _client;

    public RoomRepository(IMongoClient client, IMyMongoDbSettings dbSettings, UserManager<AppUser> userManager)
    {
        var database = client.GetDatabase(dbSettings.DatabaseName);
        _collectionUsers = database.GetCollection<AppUser>(AppVariablesExtensions.CollectionUsers);
        _collectionRooms = database.GetCollection<Room>(AppVariablesExtensions.CollectionRooms);

        _client = client;
        _userManager = userManager;
    }

    #endregion

    public async Task<OperationResult> CreateRoomAsync(CreateRoomDto request, ObjectId userId,
        CancellationToken cancellationToken)
    {
        string cleanRoomName = request.Name.ToNormalized();

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
            MemberIds = [],
            CreatedAt = DateTime.UtcNow,
            JoinRequests = []
        };

        using IClientSessionHandle session = await _client.StartSessionAsync(null, cancellationToken);

        session.StartTransaction();

        try
        {
            await _collectionRooms.InsertOneAsync(room, null, cancellationToken);

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

    public async Task<OperationResult<PagedList<Room>>> GetAllAsync(PaginationParams paginationParams,
        CancellationToken cancellationToken)
    {
        IQueryable<Room> query = _collectionRooms.AsQueryable();

        PagedList<Room> rooms = await PagedList<Room>.CreatePagedListAsync(query, paginationParams.PageNumber,
            paginationParams.PageSize, cancellationToken);
        
        return new(
            true,
            rooms,
            null
        );
    }
}