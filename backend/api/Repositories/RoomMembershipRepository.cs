using api.DTOs;
using api.DTOs.Helpers;
using api.Enums;
using api.Extensions;
using api.Interfaces;
using api.Models;
using api.Settings;
using MongoDB.Bson;
using MongoDB.Driver;

namespace api.Repositories;

public class RoomMembershipRepository : IRoomMembershipRepository
{
    #region Variables and Constructor

    private readonly IMongoCollection<AppUser> _collectionUsers;
    private readonly IMongoCollection<Room> _collectionRooms;

    public RoomMembershipRepository(IMongoClient client, IMyMongoDbSettings dbSettings)
    {
        var database = client.GetDatabase(dbSettings.DatabaseName);
        _collectionUsers = database.GetCollection<AppUser>(AppVariablesExtensions.CollectionUsers);
        _collectionRooms = database.GetCollection<Room>(AppVariablesExtensions.CollectionRooms);
        database.GetCollection<RoomMessage>(AppVariablesExtensions.CollectionRoomsChats);
        database.GetCollection<MembershipProposal>(AppVariablesExtensions.CollectionMembershipProposals);
    }

    # endregion

    public async Task<OperationResult> JoinRoomAsync(ObjectId roomId, ObjectId userId,
        CancellationToken cancellationToken)
    {
        bool isUserExist = await _collectionUsers.Find(doc => doc.Id == userId).AnyAsync(cancellationToken);

        if (!isUserExist)
        {
            return new(
                false,
                new(
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
                new(
                    ErrorCode.RoomNotFound,
                    "Target room not found."
                )
            );
        }

        if (targetRoom.OwnerId == userId)
        {
            return new(
                false,
                new(
                    ErrorCode.JoinToOwnRoom,
                    "You are already room owner."
                )
            );
        }

        if (targetRoom.RoomType == RoomType.Private)
        {
            return new(
                false,
                new(
                    ErrorCode.PrivateRoom,
                    "The given room is private. Please send request to a private room."
                )
            );
        }

        bool isAlreadyJoined =
            targetRoom.MemberIds.Contains(userId);

        if (isAlreadyJoined)
        {
            return new(
                false,
                new(
                    ErrorCode.AlreadyInRoom,
                    "This room is already joined."
                )
            );
        }

        UpdateDefinition<Room> updateDef = Builders<Room>.Update
            .AddToSet(doc => doc.MemberIds, userId)
            .Inc(doc => doc.MembersCount, 1);

        UpdateResult updateResult =
            await _collectionRooms.UpdateOneAsync(doc => doc.Id == roomId, updateDef, null, cancellationToken);

        if (updateResult.ModifiedCount == 0)
        {
            return new(
                false,
                new(
                    ErrorCode.UpdateFailed,
                    "Failed to join room."
                )
            );
        }

        return new(
            true,
            null
        );
    }

    public async Task<OperationResult> LeaveRoomAsync(ObjectId roomId, ObjectId userId,
        CancellationToken cancellationToken)
    {
        bool isUserExist = await _collectionUsers.Find(doc => doc.Id == userId).AnyAsync(cancellationToken);

        if (!isUserExist)
        {
            return new(
                false,
                new(
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
                new(
                    ErrorCode.RoomNotFound,
                    "The given room does not exist."
                )
            );
        }

        bool isRoomContainsMember =
            targetRoom.MemberIds.Contains(userId);

        if (!isRoomContainsMember)
        {
            return new(
                false,
                new(
                    ErrorCode.NotInRoom,
                    "You are not in a room."
                )
            );
        }

        if (targetRoom.OwnerId == userId)
        {
            return new(
                false,
                new(
                    ErrorCode.LeftOwnRoom,
                    "You are already room owner."
                )
            );
        }

        UpdateDefinition<Room> updateDef = Builders<Room>.Update
            .Pull(doc => doc.MemberIds, userId)
            .Inc(doc => doc.MembersCount, -1);

        UpdateResult updateResult =
            await _collectionRooms.UpdateOneAsync(doc => doc.Id == roomId, updateDef, null, cancellationToken);

        if (!updateResult.IsModifiedCountAvailable)
        {
            return new(
                false,
                new(
                    ErrorCode.UpdateFailed,
                    "Failed to leave room."
                )
            );
        }

        return new(
            true,
            null
        );
    }

    public async Task<OperationResult> RemoveMemberAsync(ObjectId roomId, ObjectId userId, string targetUserName,
        CancellationToken cancellationToken)
    {
        FindOptions options = new()
        {
            Collation = new Collation("en", strength: CollationStrength.Secondary)
        };


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

        ObjectId? targetUserId = await _collectionUsers
            .Find(doc => doc.NormalizedUserName == targetUserName, options)
            .Project(doc => doc.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (targetUserId.Equals(null))
        {
            return new(
                false,
                new(
                    ErrorCode.UserNotFound,
                    "There is no user associated with this request."
                )
            );
        }

        if (targetUserId == targetRoom.OwnerId)
        {
            return new(
                false,
                new(
                    ErrorCode.LeftOwnRoom,
                    "You cannot remove the owner of this room."
                )
            );
        }

        if (!targetRoom.MemberIds.Contains(targetUserId.Value))
        {
            return new(
                false,
                new(
                    ErrorCode.NotInRoom,
                    "Target user is not member of this room."
                )
            );
        }

        UpdateDefinition<Room> updateDef = Builders<Room>.Update
            .Pull(doc => doc.MemberIds, targetUserId.Value)
            .Inc(doc => doc.MembersCount, -1);

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

    public async Task<OperationResult<IEnumerable<MemberDto>>> GetRoomMembersAsync(ObjectId roomId,
        CancellationToken cancellationToken)
    {
        var roomData = await _collectionRooms
            .Find(doc => doc.Id == roomId)
            .Project(r => new { r.OwnerId, r.MemberIds })
            .FirstOrDefaultAsync(cancellationToken);

        if (roomData is null)
        {
            return new(
                false,
                Error: new(
                    ErrorCode.RoomNotFound,
                    "There is no room associated with this request."
                )
            );
        }

        if (roomData.MemberIds.Count == 0)
        {
            return new(
                true,
                [],
                null
            );
        }

        var users = await _collectionUsers
            .Find(doc => roomData.MemberIds.Contains(doc.Id))
            .Project(user => new
            {
                user.Id,
                user.Email,
                user.UserName
            })
            .ToListAsync(cancellationToken);

        IEnumerable<MemberDto> result = users.Select(u => new MemberDto(
            UserName: u.UserName ?? "Unknown",
            Email: u.Email ?? "Unknown",
            IsOwner: u.Id == roomData.OwnerId
        ));

        return new(
            true,
            result,
            null
        );
    }

    public async Task<bool> CheckIsMemberAsync(
        ObjectId userId,
        ObjectId roomId,
        CancellationToken ct)
        => await _collectionRooms
            .Find(r => r.Id == roomId && (r.OwnerId == userId || r.MemberIds.Contains(userId)))
            .AnyAsync(ct);

    public async Task<bool> CheckMembershipAsync(ObjectId roomId, ObjectId userId, CancellationToken cancellationToken)
    {
        return await _collectionRooms
            .Find(doc =>
                doc.Id == roomId &&
                (
                    doc.RoomType != RoomType.Private ||
                    doc.OwnerId == userId ||
                    doc.MemberIds.Contains(userId)
                )
            )
            .AnyAsync(cancellationToken);
    }

    public async Task<OperationResult<IEnumerable<RoomResponse>>> GetRoomsUserIsMemberOfAsync(ObjectId userId,
        CancellationToken cancellationToken)
    {
        IEnumerable<Room> rooms = await _collectionRooms
            .Find(doc => doc.MemberIds.Contains(userId) && doc.OwnerId != userId)
            .ToListAsync(cancellationToken);

        List<ObjectId> ownerIds = rooms.Select(item => item.OwnerId).ToList();

        var users = await _collectionUsers
            .Find(doc => ownerIds.Contains(doc.Id))
            .Project(u => new { u.Id, u.UserName })
            .ToListAsync(cancellationToken);

        var userDict = users.ToDictionary(u => u.Id, u => u.UserName);

        List<RoomResponse> roomResponses = rooms
            .Select(r =>
                Mappers
                    .ConvertRoomToRoomResponse(r, userDict.GetValueOrDefault(r.OwnerId) ?? "Unknown", true, false,
                        false))
            .ToList();

        return new(
            true,
            roomResponses,
            null
        );
    }
}