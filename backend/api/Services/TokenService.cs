using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using api.Extensions;
using api.Interfaces;
using api.Models;
using api.Settings;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace api.Services;

public class TokenService : ITokenService
{
    private readonly IMongoCollection<AppUser> _collection;
    private readonly SymmetricSecurityKey? _key; // set it as nullable by ? mark
    private readonly UserManager<AppUser> _userManager;

    public TokenService(IConfiguration config, IMongoClient client, IMyMongoDbSettings dbSettings, UserManager<AppUser> userManager)
    {
        var database = client.GetDatabase(dbSettings.DatabaseName);
        _collection = database.GetCollection<AppUser>(AppVariablesExtensions.CollectionUsers);

        string? tokenValue = config.GetValue<string>(AppVariablesExtensions.TokenKey);

        // throw exception if tokenValue is null
        _ = tokenValue ?? throw new ArgumentNullException("tokenValue cannot be null", nameof(tokenValue));

        _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(tokenValue!));

        _userManager = userManager;
    }

    public async Task<string?> CreateToken(AppUser appUser, CancellationToken cancellationToken)
    {
        _ = _key ?? throw new ArgumentNullException("_key cannot be null", nameof(_key));

        string? hashedUserId = await GenerateAndStoreHashedId(appUser.Id, cancellationToken);

        if (string.IsNullOrEmpty(hashedUserId))
            return null;

        var claims = new List<Claim> {
            new Claim(JwtRegisteredClaimNames.NameId, hashedUserId)
            // new Claim(JwtRegisteredClaimNames.Email, appUser.Email)
        };

        // Get user's roles and add them all into claims
        IList<string>? roles = await _userManager.GetRolesAsync(appUser);
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var creds = new SigningCredentials(_key, SecurityAlgorithms.HmacSha512Signature);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.Now.AddDays(7), // Set expiration days
            SigningCredentials = creds
        };

        var tokenHandler = new JwtSecurityTokenHandler();

        SecurityToken? securityToken = tokenHandler.CreateToken(tokenDescriptor);

        if (securityToken is null) return null;

        return tokenHandler.WriteToken(securityToken);
    }

    public async Task<ObjectId?> GetActualUserIdAsync(string? hashedUserId, CancellationToken cancellationToken)
    {
        if (hashedUserId is null) return null;

        ObjectId? userId = await _collection
            .AsQueryable()
            .Where(rootModel => rootModel.IdentifierHash == hashedUserId)
            .Select(rootModel => rootModel.Id)
            .SingleOrDefaultAsync(cancellationToken);

        return ValidationExtensions.ValidateObjectId(userId);
    }

    private async Task<string?> GenerateAndStoreHashedId(ObjectId userId, CancellationToken cancellationToken)
    {
        string newObjectId = ObjectId.GenerateNewId().ToString();

        string identifierHash = BCrypt.Net.BCrypt.HashPassword(newObjectId);

        UpdateDefinition<AppUser> updatedSecuredToken = Builders<AppUser>.Update
            .Set(appUser => appUser.IdentifierHash, identifierHash);
        
        UpdateResult updateResult = await _collection.UpdateOneAsync(appUser => appUser.Id == userId, updatedSecuredToken, null, cancellationToken);

        if (updateResult.ModifiedCount == 1)
            return identifierHash;

        return null;
    }
}