using api.DTOs;
using api.DTOs.Helpers;
using MongoDB.Bson;

namespace api.Interfaces;

public interface IUserRepository
{
    public Task<string> GetUserNameByIdAsync(ObjectId userId, CancellationToken cancellationToken);
    
    public Task<OperationResult<UserDto>> GetUserByIdAsync(ObjectId userId, CancellationToken cancellationToken);
}