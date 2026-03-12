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
    private readonly IMongoCollection<AudioMessage> _collectionAudios;

    public RoomMessageRepository(IMongoClient client, IMyMongoDbSettings dbSettings)
    {
        var database = client.GetDatabase(dbSettings.DatabaseName);
        _collectionUsers = database.GetCollection<AppUser>(AppVariablesExtensions.CollectionUsers);
        _collectionRooms = database.GetCollection<Room>(AppVariablesExtensions.CollectionRooms);
        _collectionChats = database.GetCollection<RoomChat>(AppVariablesExtensions.CollectionRoomsChats);
        _collectionAudios = database.GetCollection<AudioMessage>(AppVariablesExtensions.CollectionAudios);
        database.GetCollection<MembershipProposal>(AppVariablesExtensions.CollectionMembershipProposals);
    }

    #endregion

    public async Task<OperationResult<ChatItemDto>> SavedMessageAsync(MessageRequest req, ObjectId userId,
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
            new ChatItemDto(
                chat.Id.ToString()!,
                ChatItemType.Text,
                senderUserName,
                chat.Message,
                null,
                null,
                chat.TimeStamp
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

        var textFilter = Builders<RoomChat>.Filter.Eq(m => m.RoomId, roomId);
        var audioFilter = Builders<AudioMessage>.Filter.Eq(a => a.RoomId, roomId);

        if (messageParams.LastMessageId.HasValue)
        {
            textFilter &= Builders<RoomChat>.Filter.Lt(m => m.Id, messageParams.LastMessageId.Value);
            audioFilter &= Builders<AudioMessage>.Filter.Lt(a => a.Id, messageParams.LastMessageId.Value);
        }

        int fetchSize = messageParams.Limit * 2;

        List<RoomChat> textMessages =
            await _collectionChats
                .Find(textFilter)
                .SortByDescending(m => m.Id)
                .Limit(fetchSize)
                .ToListAsync(cancellationToken);

        List<AudioMessage> audioMessages =
            await _collectionAudios
                .Find(audioFilter)
                .SortByDescending(a => a.Id)
                .Limit(fetchSize)
                .ToListAsync(cancellationToken);

        List<ObjectId> senderIds = textMessages
            .Select(item => item.SenderId)
            .Concat(audioMessages.Select(item => item.UploaderId))
            .Distinct()
            .ToList();

        var users = await _collectionUsers
            .Find(doc => senderIds.Contains(doc.Id))
            .Project(u => new { u.Id, u.UserName })
            .ToListAsync(cancellationToken);

        var userDict = users.ToDictionary(u => u.Id, u => u.UserName);

        IEnumerable<ChatItemDto> textDtos = textMessages.Select(m => new ChatItemDto(
            m.Id.ToString()!,
            ChatItemType.Text,
            userDict.GetValueOrDefault(m.SenderId) ?? "Unknown",
            m.Message,
            null,
            null,
            m.TimeStamp
        ));

        IEnumerable<ChatItemDto> audioDtos = audioMessages.Select(a => new ChatItemDto(
            a.Id.ToString()!,
            a.Type == AudioType.Voice ? ChatItemType.Voice : ChatItemType.Audio,
            userDict.GetValueOrDefault(a.UploaderId) ?? "Unknown",
            null,
            a.Duration,
            a.FileSize,
            a.CreatedAt
        ));

        List<ChatItemDto> combined = textDtos
            .Concat(audioDtos)
            .OrderByDescending(item => item.CreatedAt)
            .Take(messageParams.Limit + 1)
            .ToList();

        bool hasMore = combined.Count > messageParams.Limit;

        if (hasMore)
        {
            combined.RemoveAt(combined.Count - 1);
        }

        var result = new MessagesPageDto(combined, hasMore);

        return new(
            true,
            result,
            null
        );
    }
}