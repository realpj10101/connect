using api.DTOs;
using api.DTOs.Helpers;
using api.Extensions;
using api.Interfaces;
using api.Models;
using api.Settings;
using Microsoft.AspNetCore.Identity;
using MongoDB.Bson;
using MongoDB.Driver;

namespace api.Repositories;

public class AccountRepository : IAccountRepository
{
    #region Variables and Constructor

    private readonly IMongoCollection<AppUser>? _collection;
    private readonly UserManager<AppUser> _userManager;
    private readonly ITokenService _tokenService;

    public AccountRepository(IMongoClient client, IMyMongoDbSettings dbSettings, UserManager<AppUser> userManager,
        ITokenService tokenService)
    {
        var database = client.GetDatabase(dbSettings.DatabaseName);
        _collection = database.GetCollection<AppUser>(AppVariablesExtensions.CollectionUsers);
        _userManager = userManager;
        _tokenService = tokenService;
    }

    #endregion

    public async Task<OperationResult<LoggedInDto>> RegisterAsync(RegisterDto request,
        CancellationToken cancellationToken)
    {
        AppUser appUser = Mappers.ConvertRegisterDtoToAppUser(request);

        IdentityResult? userCreationResult = await _userManager.CreateAsync(appUser, request.Password);

        if (userCreationResult.Succeeded)
        {
            IdentityResult? roleResult = await _userManager.AddToRoleAsync(appUser, "member");

            if (!roleResult.Succeeded)
            {
                return new(
                    false,
                    Error: new CustomError(
                        ErrorCode.NetIdentityFailed,
                        "Failed to create role"
                    )
                );
            }

            string? token = await _tokenService.CreateToken(appUser, cancellationToken);

            if (!string.IsNullOrEmpty(token))
            {
                return new(
                    true,
                    Mappers.ConvertAppUserToLoggedInDto(appUser, token),
                    null
                );
            }
        }
        else
        {
            string? errorMessage = userCreationResult.Errors.FirstOrDefault()?.Description;

            return new OperationResult<LoggedInDto>(
                IsSuccess: false,
                Error: new CustomError(
                    Code: ErrorCode.NetIdentityFailed,
                    Message: errorMessage
                )
            );
        }

        return new OperationResult<LoggedInDto>(
            IsSuccess: false,
            Error: new CustomError(
                Code: ErrorCode.Failed,
                Message: "Account creation failed. Try again later."
            )
        );
    }

    public async Task<OperationResult<LoggedInDto>> LoginAsync(LoginDto request, CancellationToken cancellationToken)
    {
        AppUser? appUser = await _userManager.FindByEmailAsync(request.Email);

        if (appUser is null)
        {
            return new(
                false,
                Error: new CustomError(
                    ErrorCode.WrongCreds,
                    "Wrong Credentials."
                )
            );
        }

        bool isPassCorrect = await _userManager.CheckPasswordAsync(appUser, request.Password);

        if (!isPassCorrect)
        {
            return new(
                false,
                Error: new CustomError(
                    ErrorCode.WrongCreds,
                    "Wrong Credentials."
                )
            );
        }

        string? token = await _tokenService.CreateToken(appUser, cancellationToken);

        if (!string.IsNullOrEmpty(token))
        {
            return new OperationResult<LoggedInDto>(
                true,
                Mappers.ConvertAppUserToLoggedInDto(appUser, token),
                Error: null
            );
        }

        return new OperationResult<LoggedInDto>(
            false,
            Error: new CustomError(
                Code: ErrorCode.Failed,
                Message: "Operation failed"
            )
        );
    }

    public async Task<OperationResult<LoggedInDto>> ReloadLoggedInUserAsync(string hashedUserId, string token,
        CancellationToken cancellationToken)
    {
        ObjectId? userId = await _tokenService.GetActualUserIdAsync(hashedUserId, cancellationToken);

        if (userId is null)
        {
            return new(
                false,
                Error: new CustomError(
                    ErrorCode.Failed,
                    "Unable to retrieve user info."
                )
            );
        }

        AppUser appUser = await _collection.Find<AppUser>(appUser => appUser.Id == userId)
            .FirstOrDefaultAsync(cancellationToken);

        if (appUser is null)
        {
            return new(
                false,
                Error: new CustomError(
                    ErrorCode.UserNotFound,
                    "User not found."
                )
            );
        }

        return new(
            true,
            Mappers.ConvertAppUserToLoggedInDto(appUser, token),
            null
        );
    }
}