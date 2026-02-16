using api.Extensions;
using api.Interfaces;
using api.Models;
using api.Settings;
using Microsoft.AspNetCore.Identity;
using MongoDB.Bson;
using MongoDB.Driver;

namespace api.Repositories;

public class UserRepository : IUserRepository
{
    #region Variables and Constructor

    private readonly IMongoCollection<AppUser> _collectionUsers;
    private readonly IMongoCollection<Room> _collectionRooms;
    private readonly UserManager<AppUser> _userManager;
    private readonly IMongoClient _client;

    public UserRepository(IMongoClient client, IMyMongoDbSettings dbSettings, UserManager<AppUser> userManager)
    {
        var database = client.GetDatabase(dbSettings.DatabaseName);
        _collectionUsers = database.GetCollection<AppUser>(AppVariablesExtensions.CollectionUsers);
        _collectionRooms = database.GetCollection<Room>(AppVariablesExtensions.CollectionRooms);

        _client = client;
        _userManager = userManager;
    }

    #endregion
    
    public async Task<string?> GetUserNameByIdAsync(ObjectId userId, CancellationToken cancellationToken)
    {
        string? userName = await _collectionUsers
            .Find(doc => doc.Id == userId)
            .Project(doc => doc.UserName)
            .FirstOrDefaultAsync(cancellationToken);
        
        if (userName is null)
            return null;

        return userName;
    }
}