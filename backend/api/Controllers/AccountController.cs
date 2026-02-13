using api.Controllers.Helpers;
using api.DTOs;
using api.DTOs.Helpers;
using api.Extensions;
using api.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

public class AccountController(IAccountRepository accountRepository) : BaseApiController
{
    [HttpPost("register")]
    public async Task<ActionResult<LoggedInDto>> Register(RegisterDto request, CancellationToken cancellationToken)
    {
        if (request.Password != request.ConfirmPassword)
        {
            return BadRequest("Passwords do not match");
        }

        OperationResult<LoggedInDto> opResult = await accountRepository.RegisterAsync(request, cancellationToken);

        return opResult.IsSuccess
            ? opResult.Result
            : opResult.Error?.Code switch
            {
                ErrorCode.NetIdentityFailed => BadRequest(opResult.Error.Message),
                ErrorCode.Failed => BadRequest(opResult.Error.Message),
                _ => BadRequest("Operation failed! Try again or contact support.")
            };
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoggedInDto>> Login(LoginDto request, CancellationToken cancellationToken)
    {
        OperationResult<LoggedInDto> opResult = await accountRepository.LoginAsync(request, cancellationToken);

        return opResult.IsSuccess
            ? opResult.Result
            : opResult.Error?.Code switch
            {
                ErrorCode.WrongCreds => BadRequest(opResult.Error.Message),
                ErrorCode.Failed => BadRequest(opResult.Error.Message),
                _ => BadRequest("Operation failed! Try again or contact support.")
            };
    }

    [HttpGet]
    public async Task<ActionResult<LoggedInDto>> ReloadLoggedInUser(CancellationToken cancellationToken)
    {
        string token = null;

        bool isTokenValid = HttpContext.Request.Headers.TryGetValue("Authorization", out var authHeader);

        if (isTokenValid)
            token = authHeader.ToString().Split(' ').Last();

        if (string.IsNullOrEmpty(token))
            return Unauthorized("Token is expired or invalid. Login again.");

        string? hashedUserId = User.GetHashedUserId();
        if (string.IsNullOrEmpty(hashedUserId))
            return BadRequest("No user found with this user Id");

        OperationResult<LoggedInDto> opResult =
            await accountRepository.ReloadLoggedInUserAsync(hashedUserId, token, cancellationToken);

        return opResult.IsSuccess
            ? opResult.Result
            : opResult.Error?.Code switch
            {
                ErrorCode.Failed => BadRequest(opResult.Error.Message),
                ErrorCode.UserNotFound => NotFound(opResult.Error.Message),
                _ => BadRequest("Operation failed! Try again or contact support.")
            };
    }
}