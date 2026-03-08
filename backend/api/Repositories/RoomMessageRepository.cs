using api.DTOs;
using api.DTOs.Helpers;
using api.Enums;
using api.Extensions;
using api.Helpers;
using api.Interfaces;
using api.Models;
using api.Settings;
using MongoDB.Bson;
using MongoDB.Driver;

namespace api.Repositories;

public class RoomMessageRepository : IRoomMessageRepository
{
    #region Variables and Constructor

    private readonly IMongoCollection<AppUser> _collectionUsers;
    private readonly IMongoCollection<Room> _collectionRooms;
    private readonly IMongoCollection<RoomChat> _collectionChats;

    public RoomMessageRepository(IMongoClient client, IMyMongoDbSettings dbSettings)
    {
        var database = client.GetDatabase(dbSettings.DatabaseName);
        _collectionUsers = database.GetCollection<AppUser>(AppVariablesExtensions.CollectionUsers);
        _collectionRooms = database.GetCollection<Room>(AppVariablesExtensions.CollectionRooms);
        _collectionChats = database.GetCollection<RoomChat>(AppVariablesExtensions.CollectionRoomsChats);
        database.GetCollection<MembershipProposal>(AppVariablesExtensions.CollectionMembershipProposals);
    }

    #endregion

    public async Task<OperationResult<MessageResponseDto>> SavedMessageAsync(MessageRequest req, ObjectId userId,
        ObjectId roomId, CancellationToken cancellationToken)
    {
        if (userId == ObjectId.Empty || roomId == ObjectId.Empty)
        {
            return new(
                false,
                Error: new CustomError(
                    ErrorCode.InvalidData,
                    "Invalid user ID or room ID."
                )
            );
        }

        string? senderUserName = await _collectionUsers.Find(user => user.Id == userId)
            .Project(user => user.UserName)
            .FirstOrDefaultAsync(cancellationToken);

        if (senderUserName is null)
        {
            return new(
                false,
                Error: new CustomError(
                    ErrorCode.UserNotFound,
                    "The given user does not exist."
                )
            );
        }

        Room? targetRoom =
            await _collectionRooms.Find(room => room.Id == roomId).FirstOrDefaultAsync(cancellationToken);

        if (targetRoom is null)
        {
            return new(
                false,
                Error: new(
                    ErrorCode.RoomNotFound,
                    "The given room does not exist."
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
                    "Room is private. You cannot send message. Please join the room first."
                )
            );
        }

        RoomChat chat = new()
        {
            Id = ObjectId.GenerateNewId(),
            RoomId = roomId,
            SenderId = userId,
            Message = req.Message,
            TimeStamp = DateTime.UtcNow
        };

        await _collectionChats.InsertOneAsync(chat, null, cancellationToken);

        return new(
            true,
            new MessageResponseDto(
                Id: chat.Id.ToString()!,
                Message: req.Message,
                SenderUserName: senderUserName,
                TimeStamp: chat.TimeStamp
            ),
            null
        );
    }

    public async Task<OperationResult<MessagesPageDto>> GetAllMessagesAsync(ObjectId roomId,
        ObjectId userId, MessageParams messageParams,
        CancellationToken cancellationToken)
    {
        bool isUserExist = await _collectionUsers.Find(doc => doc.Id == userId).AnyAsync(cancellationToken);

        if (!isUserExist)
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
                    "You do not have permission to access this room. Please join this room first."
                )
            );
        }

        var filter = Builders<RoomChat>.Filter.Eq(m => m.RoomId, roomId);

        if (messageParams.LastMessageId.HasValue)
        {
            filter &= Builders<RoomChat>.Filter.Lt(m => m.Id, messageParams.LastMessageId.Value);
        }

        List<RoomChat> messages =
            await _collectionChats
                .Find(filter)
                .SortByDescending(m => m.Id)
                .Limit(messageParams.Limit + 1)
                .ToListAsync(cancellationToken);

        bool hasMore = messages.Count() > messageParams.Limit;

        if (hasMore)
        {
            messages.RemoveAt(messages.Count() - 1);
        }

        List<ObjectId> senderIds = messages.Select(item => item.SenderId).Distinct().ToList();

        var users = await _collectionUsers
            .Find(doc => senderIds.Contains(doc.Id))
            .Project(u => new { u.Id, u.UserName })
            .ToListAsync(cancellationToken);

        var userDict = users.ToDictionary(u => u.Id, u => u.UserName);

        List<MessageResponseDto> messagesDto = messages.Select(m => new MessageResponseDto(
            m.Id.ToString()!,
            m.Message,
            userDict.GetValueOrDefault(m.SenderId) ?? "Unknown",
            m.TimeStamp
        )).ToList();

        MessagesPageDto res = new(
            messagesDto,
            hasMore
        );

        return new(
            true,
            res,
            null
        );
    }
}