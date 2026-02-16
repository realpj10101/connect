using api.DTOs.Helpers;
using MongoDB.Bson;

namespace api.Interfaces;

public interface IUserRepository
{
    public Task<string> GetUserNameByIdAsync(ObjectId userId, CancellationToken cancellationToken);
}