using api.Controllers.Helpers;
using api.DTOs;
using api.DTOs.Helpers;
using api.Extensions;
using api.Interfaces;
using api.Models;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;

namespace api.Controllers;

public class RoomController(IRoomRepository roomRepository, ITokenService tokenService) : BaseApiController
{
    [HttpPost("create")]
    public async Task<ActionResult<Response>> CreateRoom(CreateRoomDto request, CancellationToken cancellationToken)
    {
        string? hashedUserId = User.GetHashedUserId();

        if (hashedUserId is null)
            return Unauthorized("You are not logged in. Please login again.");

        ObjectId? userId = await tokenService.GetActualUserIdAsync(hashedUserId, cancellationToken);

        if (userId is null)
            return Unauthorized("You are not logged in. Please login again.");

        OperationResult opResult = await roomRepository.CreateRoomAsync(request, userId.Value, cancellationToken);

        return opResult.IsSuccess
            ? Ok(new Response("Room created successfully!"))
            : opResult.Error?.Code switch
            {
                ErrorCode.UserNotFound => BadRequest(opResult.Error.Message),
                ErrorCode.RoomAlreadyExist => BadRequest(opResult.Error.Message),
                ErrorCode.InvalidType => BadRequest(opResult.Error.Message),
                ErrorCode.TransactionFailed => BadRequest(opResult.Error.Message),
                _ => BadRequest("Operation failed! Try again later or contact support.")
            };
    }
}