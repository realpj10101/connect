using api.DTOs;
using api.DTOs.Helpers;
using api.Helpers;
using api.Models;
using MongoDB.Bson;

namespace api.Interfaces;

public interface IRoomManagementRepository
{
    public Task<OperationResult> CreateRoomAsync(CreateRoomDto request, ObjectId userId,
        CancellationToken cancellationToken);

    public Task<OperationResult<PagedList<Room>>> GetAllAsync(RoomParams roomParams,
        CancellationToken cancellationToken);

    public Task<OperationResult<RoomResponse>> GetRoomByIdAsync(ObjectId roomId, ObjectId userId,
        CancellationToken cancellationToken);

    public Task<bool> CheckIsOwnRoomAsync(ObjectId userId, ObjectId roomId, CancellationToken cancellationToken);

    public Task<OperationResult> UpdateRoomAsync(ObjectId roomId, ObjectId userId,
        RoomUpdateDto request, CancellationToken cancellationToken);
    
    public Task<OperationResult<IEnumerable<RoomResponse>>> GetRoomsCreatedByUserAsync(ObjectId userId, string userName, CancellationToken cancellationToken);
}