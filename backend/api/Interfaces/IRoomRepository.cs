using api.DTOs;
using api.DTOs.Helpers;
using MongoDB.Bson;

namespace api.Interfaces;

public interface IRoomRepository
{
    public Task<OperationResult> CreateRoomAsync(CreateRoomDto request, ObjectId userId, CancellationToken cancellationToken);
}