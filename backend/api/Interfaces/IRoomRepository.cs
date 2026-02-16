using api.DTOs;
using api.DTOs.Helpers;
using api.Helpers;
using api.Models;
using MongoDB.Bson;

namespace api.Interfaces;

public interface IRoomRepository
{
    public Task<OperationResult> CreateRoomAsync(CreateRoomDto request, ObjectId userId, CancellationToken cancellationToken);
    public Task<OperationResult<PagedList<Room>>> GetAllAsync(PaginationParams paginationParams, CancellationToken cancellationToken);
}