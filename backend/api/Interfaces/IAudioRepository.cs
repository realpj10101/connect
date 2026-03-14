using api.DTOs.Helpers;
using api.Models;
using MongoDB.Bson;

namespace api.Interfaces;

public interface IAudioRepository
{
    public Task<OperationResult<RoomMessage>> UploadVoiceAsync(IFormFile voiceFile, ObjectId roomId, ObjectId userId,
        CancellationToken cancellationToken);

    public Task<OperationResult<RoomMessage>> UploadAudioAsync(IFormFile audioFile, ObjectId roomId, ObjectId userId,
        CancellationToken cancellationToken);

    public Task<OperationResult<RoomMessage>> GetAudioByIdAsync(ObjectId audioId, ObjectId userId,
        CancellationToken cancellationToken);
}