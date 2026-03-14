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

public class RoomJoinRequestRepository : IRoomJoinRequestRepository
{
    #region Variables and Constructor

    private readonly IMongoCollection<AppUser> _collectionUsers;
    private readonly IMongoCollection<Room> _collectionRooms;
    private readonly IMongoCollection<MembershipProposal> _collectionMembershipProposals;
    private readonly IMongoClient _client;

    public RoomJoinRequestRepository(IMongoClient client, IMyMongoDbSettings dbSettings)
    {
        var database = client.GetDatabase(dbSettings.DatabaseName);
        _collectionUsers = database.GetCollection<AppUser>(AppVariablesExtensions.CollectionUsers);
        _collectionRooms = database.GetCollection<Room>(AppVariablesExtensions.CollectionRooms);
        database.GetCollection<RoomMessage>(AppVariablesExtensions.CollectionRoomsChats);
        _collectionMembershipProposals =
            database.GetCollection<MembershipProposal>(AppVariablesExtensions.CollectionMembershipProposals);

        _client = client;
    }

    #endregion

    public async Task<OperationResult> JoinRequestAsync(ObjectId roomId, ObjectId userId,
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

        Room targetRoom = await _collectionRooms.Find(doc => doc.Id == roomId).FirstOrDefaultAsync(cancellationToken);

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

        if (targetRoom.RoomType == RoomType.Public)
        {
            return new(
                false,
                new(
                    ErrorCode.PublicRoom,
                    "The given room is public."
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

        bool isAlreadySentRequest =
            await _collectionMembershipProposals.Find(doc =>
                    doc.RoomId == roomId && doc.UserId == userId && doc.Status == JoinStatus.Pending)
                .AnyAsync(cancellationToken);

        if (isAlreadySentRequest)
        {
            return new(
                false,
                new(
                    ErrorCode.AlreadySentRequest,
                    "You have already sent request."
                )
            );
        }

        var joinReq = new MembershipProposal()
        {
            Id = ObjectId.GenerateNewId(),
            RoomId = roomId,
            UserId = userId,
            Status = JoinStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        await _collectionMembershipProposals.InsertOneAsync(joinReq, null, cancellationToken);

        return new(
            true,
            null
        );
    }

    public async Task<OperationResult> ApproveRequestAsync(ObjectId joinRequestId, ObjectId userId,
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

        MembershipProposal? targetMembershipProp = await _collectionMembershipProposals
            .Find(doc => doc.Id == joinRequestId)
            .FirstOrDefaultAsync(cancellationToken);

        if (targetMembershipProp is null)
        {
            return new(
                false,
                new(
                    ErrorCode.MembershipPropNotExist,
                    "Target request does not exist."
                )
            );
        }

        if (targetMembershipProp.Status == JoinStatus.Approved)
        {
            return new(
                false,
                new(
                    ErrorCode.AlreadyAccepted,
                    "Target request is already approved."
                )
            );
        }

        Room targetRoom = await _collectionRooms.Find(doc => doc.Id == targetMembershipProp.RoomId)
            .FirstOrDefaultAsync(cancellationToken);

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
                    "You are not the owner of this room for approve the request."
                )
            );
        }

        using IClientSessionHandle session = await _client.StartSessionAsync(null, cancellationToken);

        session.StartTransaction();

        try
        {
            #region Update proposal

            UpdateDefinition<MembershipProposal> updatePropDef = Builders<MembershipProposal>.Update
                .Set(doc => doc.Status, JoinStatus.Approved);

            UpdateResult updatePropResult =
                await _collectionMembershipProposals
                    .UpdateOneAsync(session, doc => doc.Id == joinRequestId, updatePropDef, null, cancellationToken);

            if (!updatePropResult.IsModifiedCountAvailable || updatePropResult.ModifiedCount == 0)
                throw new Exception("Failed to update proposal.");

            #endregion

            #region Update Room

            UpdateDefinition<Room> updateRoomDef = Builders<Room>.Update
                .AddToSet(doc => doc.MemberIds, targetMembershipProp.UserId)
                .Inc(doc => doc.MembersCount, 1);

            UpdateResult updateRoomResult = await
                _collectionRooms.UpdateOneAsync(session, doc => doc.Id == targetRoom.Id, updateRoomDef, null,
                    cancellationToken);

            if (!updateRoomResult.IsModifiedCountAvailable || updateRoomResult.ModifiedCount == 0)
                throw new Exception("Failed to update room.");

            #endregion

            await session.CommitTransactionAsync(cancellationToken);
        }
        catch (Exception e)
        {
            await session.AbortTransactionAsync(cancellationToken);

            return new(
                false,
                new(
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

    public async Task<OperationResult> RejectRequestAsync(ObjectId joinRequestId, ObjectId userId,
        CancellationToken cancellationToken)
    {
        MembershipProposal targetProp = await _collectionMembershipProposals.Find(doc => doc.Id == joinRequestId)
            .FirstOrDefaultAsync(cancellationToken);

        if (targetProp is null)
        {
            return new(
                false,
                new(
                    ErrorCode.MembershipPropNotExist,
                    "Target request does not exist."
                )
            );
        }

        if (targetProp.Status == JoinStatus.Rejected)
        {
            return new(
                false,
                new(
                    ErrorCode.AlreadyRejected,
                    "Target request is already rejected."
                )
            );
        }

        Room? targetRoom = await _collectionRooms.Find(doc => doc.Id == targetProp.RoomId)
            .FirstOrDefaultAsync(cancellationToken);

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
                    "You are not the owner of this room for rejection."
                )
            );
        }

        UpdateDefinition<MembershipProposal> updatePropDef = Builders<MembershipProposal>.Update
            .Set(doc => doc.Status, JoinStatus.Rejected);

        UpdateResult updatePropResult =
            await _collectionMembershipProposals
                .UpdateOneAsync(doc => doc.Id == joinRequestId, updatePropDef, null, cancellationToken);

        if (!updatePropResult.IsModifiedCountAvailable || updatePropResult.ModifiedCount == 0)
        {
            return new(
                false,
                new(
                    ErrorCode.UpdateFailed,
                    "Failed to update proposal."
                )
            );
        }

        return new(
            true,
            null
        );
    }

    public async Task<bool> CheckHasPendingRequestsAsync(ObjectId userId, ObjectId roomId,
        CancellationToken cancellationToken)
        => await _collectionMembershipProposals
            .Find(doc => doc.UserId == userId && doc.RoomId == roomId && doc.Status == JoinStatus.Pending)
            .AnyAsync(cancellationToken);

    public async Task<OperationResult<IEnumerable<MembershipProposalResponse>>> GetAllJoinRequestsAsync(ObjectId roomId,
        ObjectId userId, CancellationToken cancellationToken)
    {
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

        if (targetRoom.OwnerId != userId)
        {
            return new(
                false,
                Error: new(
                    ErrorCode.NotRoomOwner,
                    "You are not the owner of this room."
                )
            );
        }

        IEnumerable<MembershipProposal> membershipProposals = await _collectionMembershipProposals
            .Find(doc => doc.RoomId == roomId && doc.Status == JoinStatus.Pending)
            .ToListAsync(cancellationToken);

        if (!membershipProposals.Any())
        {
            return new(
                true,
                [],
                null
            );
        }

        // 1. Collect user IDs
        IEnumerable<ObjectId> userIds = membershipProposals.Select(doc => doc.UserId).ToList();

        // 2. Fetch all users in one query
        var users = await _collectionUsers
            .Find(doc => userIds.Contains(doc.Id))
            .Project(u => new { u.Id, u.UserName })
            .ToListAsync(cancellationToken);

        // 3. Build dictionary for fast lookup
        var userMap = users.ToDictionary(u => u.Id, u => u.UserName);

        // 4. Map proposals to responses
        var responses = membershipProposals.Select(p => new MembershipProposalResponse(
            Id: p.Id.Value.ToString(),
            SenderName: userMap.GetValueOrDefault(p.UserId, "Unknown"),
            Status: p.Status,
            CreatedAt: p.CreatedAt
        ));

        return new(
            true,
            responses,
            null
        );
    }
}