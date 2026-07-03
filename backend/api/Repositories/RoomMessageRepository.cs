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
    private readonly IMongoCollection<RoomMessage> _collectionChats;
    private readonly IFileClassifierService _fileClassifierService;
    private readonly IVideoService _videoService;
    private readonly IPhotoService _photoService;

    public RoomMessageRepository(IMongoClient client, IMyMongoDbSettings dbSettings,
        IFileClassifierService fileClassifierService, IVideoService videoService, IPhotoService photoService)
    {
        var database = client.GetDatabase(dbSettings.DatabaseName);
        _collectionUsers = database.GetCollection<AppUser>(AppVariablesExtensions.CollectionUsers);
        _collectionRooms = database.GetCollection<Room>(AppVariablesExtensions.CollectionRooms);
        _collectionChats = database.GetCollection<RoomMessage>(AppVariablesExtensions.CollectionRoomsChats);
        database.GetCollection<MembershipProposal>(AppVariablesExtensions.CollectionMembershipProposals);

        _fileClassifierService = fileClassifierService;
        _videoService = videoService;
        _photoService = photoService;
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

        RoomMessage message = new()
        {
            Id = ObjectId.GenerateNewId(),
            RoomId = roomId,
            SenderId = userId,
            Type = ChatItemType.Text,
            Message = req.Message,
            TimeStamp = DateTime.UtcNow
        };

        await _collectionChats.InsertOneAsync(message, null, cancellationToken);

        return new(
            true,
            new ChatItemDto(
                message.Id.ToString()!,
                ChatItemType.Text,
                senderUserName,
                message.Message,
                null,
                null,
                message.TimeStamp
            ),
            null
        );
    }

    public async Task<OperationResult<ChatItemDto>> UploadMediaAsync(IFormFile file, ObjectId userId, ObjectId roomId,
        CancellationToken cancellationToken)
    {
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

        ChatItemType fileType = _fileClassifierService.DetectFileType(file);

        ObjectId mediaId = ObjectId.GenerateNewId();

        if (fileType == ChatItemType.Image)
        {
            string[]? photoUrls = await _photoService.AddPhotoToDiskAsync(file, mediaId);

            if (photoUrls is null)
            {
                return new(
                    false,
                    Error: new(
                        ErrorCode.SaveToDiskFailed,
                        "Save media to disk failed."
                    )
                );
            }

            Photo photo = Mappers.ConvertPhotoUrlsToPhoto(photoUrls);

            RoomMessage roomMessage = new()
            {
                Id = mediaId,
                RoomId = roomId,
                SenderId = userId,
                Type = ChatItemType.Image,
                FileSize = file.Length,
                Photo = photo,
                TimeStamp = DateTime.UtcNow,
                MimeType = file.ContentType
            };

            await _collectionChats.InsertOneAsync(roomMessage, null, cancellationToken);

            ChatItemDto res = Mappers.ConvertRoomMessageToChatItemDto(roomMessage, senderUserName, ChatItemType.Image);

            return new(
                true,
                res,
                null
            );
        }

        if (fileType == ChatItemType.Video)
        {
            string? videoUrl = await _videoService.SaveVideoToDiskAsync(file, userId, mediaId);

            if (videoUrl is null)
            {
                return new(
                    false,
                    Error: new(
                        ErrorCode.SaveToDiskFailed,
                        "Save media to disk failed."
                    )
                );
            }

            RoomMessage roomMessage = new()
            {
                Id = mediaId,
                RoomId = roomId,
                SenderId = userId,
                Type = ChatItemType.Video,
                FilePath = videoUrl,
                FileSize = file.Length,
                TimeStamp = DateTime.UtcNow,
                MimeType = file.ContentType
            };

            await _collectionChats.InsertOneAsync(roomMessage, null, cancellationToken);

            ChatItemDto res = Mappers.ConvertRoomMessageToChatItemDto(roomMessage, senderUserName, ChatItemType.Video);

            return new(
                true,
                res,
                null
            );
        }

        return new(
            false,
            Error: new(
                ErrorCode.InvalidFileType,
                "Upload valid file type."
            )
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

        var textFilter = Builders<RoomMessage>.Filter.Eq(m => m.RoomId, roomId);

        if (messageParams.LastMessageId.HasValue)
        {
            textFilter &= Builders<RoomMessage>.Filter.Lt(m => m.Id, messageParams.LastMessageId.Value);
        }

        int fetchSize = messageParams.Limit * 2;

        List<RoomMessage> textMessages =
            await _collectionChats
                .Find(textFilter)
                .SortByDescending(m => m.Id)
                .Limit(fetchSize)
                .ToListAsync(cancellationToken);

        List<ObjectId> senderIds = textMessages
            .Select(item => item.SenderId)
            .Distinct()
            .ToList();

        var users = await _collectionUsers
            .Find(doc => senderIds.Contains(doc.Id))
            .Project(u => new { u.Id, u.UserName })
            .ToListAsync(cancellationToken);

        var userDict = users.ToDictionary(u => u.Id, u => u.UserName);

        IEnumerable<ChatItemDto> textDtos = textMessages.Select(m => new ChatItemDto(
            m.Id.ToString()!,
            m.Type,
            userDict.GetValueOrDefault(m.SenderId) ?? "Unknown",
            m.Message,
            m.Duration,
            m.FileSize,
            m.TimeStamp
        ));

        List<ChatItemDto> combined = textDtos
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

    public async Task<OperationResult<RoomMessage>> GetMessageByIdAsync(ObjectId messageId, ObjectId userId,
        CancellationToken cancellationToken)
    {
        RoomMessage targetMessage =
            await _collectionChats.Find(doc => doc.Id == messageId).FirstOrDefaultAsync(cancellationToken);

        if (targetMessage is null)
        {
            return new(
                false,
                Error: new CustomError(
                    ErrorCode.MessageNotFound,
                    "Audio not found."
                )
            );
        }

        Room? targetRoom = await _collectionRooms.Find(doc => doc.Id == targetMessage.RoomId)
            .FirstOrDefaultAsync(cancellationToken);
        
        if (targetRoom is null)
        {
            return new(
                false,
                Error: new CustomError(
                    ErrorCode.RoomNotFound,
                    "Room not found."
                )
            );
        }

        if (targetRoom.RoomType is RoomType.Private && !targetRoom.MemberIds.Contains(userId) &&
            targetRoom.OwnerId != userId)
        {
            return new(
                false,
                Error: new CustomError(
                    ErrorCode.NotRoomMember,
                    "You are not the member of this room. Please join this room first."
                )
            );
        }
        
        return new(
            true,
            targetMessage,
            null
        );
    }
}