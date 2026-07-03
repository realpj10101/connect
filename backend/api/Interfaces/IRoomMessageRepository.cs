using api.DTOs;
using api.DTOs.Helpers;
using api.Helpers;
using api.Models;
using MongoDB.Bson;

namespace api.Interfaces;

public interface IRoomMessageRepository
{
    public Task<OperationResult<ChatItemDto>> SavedMessageAsync(MessageRequest req, ObjectId userId,
        ObjectId roomId, CancellationToken cancellationToken);

    public Task<OperationResult<ChatItemDto>> UploadMediaAsync(IFormFile file, ObjectId userId,
        ObjectId roomId, CancellationToken cancellationToken);

    public Task<OperationResult<MessagesPageDto>> GetAllMessagesAsync(ObjectId roomId, ObjectId userId,
        MessageParams messageParams, CancellationToken cancellationToken);
    
    public Task<OperationResult<RoomMessage>> GetMessageByIdAsync(ObjectId messageId, ObjectId userId,
        CancellationToken cancellationToken);
}