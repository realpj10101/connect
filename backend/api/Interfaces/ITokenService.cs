using api.Models;
using MongoDB.Bson;

namespace api.Interfaces;

public interface ITokenService
{
    public Task<string?> CreateToken(AppUser appUser, CancellationToken cancellationToken);
    public Task<ObjectId?> GetActualUserIdAsync(string? hashedUserId, CancellationToken cancellationToken);
}