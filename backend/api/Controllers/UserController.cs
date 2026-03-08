using api.Controllers.Helpers;
using api.DTOs;
using api.DTOs.Helpers;
using api.Extensions;
using api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;

namespace api.Controllers;

[Authorize]
public class UserController(
    ITokenService tokenService,
    IUserRepository userRepository) : BaseApiController
{
    [HttpGet("get-user-by-id")]
    public async Task<ActionResult<UserDto>> GetUserById(CancellationToken cancellationToken)
    {
        string? hashedUserId = User.GetHashedUserId();

        if (hashedUserId is null)
            return Unauthorized("You are not logged in. Please login again.");

        ObjectId? userId = await tokenService.GetActualUserIdAsync(hashedUserId, cancellationToken);

        if (userId is null)
            return Unauthorized("You are not logged in. Please login again.");

        OperationResult<UserDto> opResult = await userRepository.GetUserByIdAsync(userId.Value, cancellationToken);

        return opResult.IsSuccess
            ? Ok(opResult.Result)
            : BadRequest(opResult.Error?.Message);
    }
}