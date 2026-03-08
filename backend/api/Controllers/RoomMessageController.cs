using api.Controllers.Helpers;
using api.DTOs;
using api.DTOs.Helpers;
using api.Extensions;
using api.Helpers;
using api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;

namespace api.Controllers;

[Authorize]
public class RoomMessageController(
    ITokenService tokenService,
    IRoomMessageRepository roomMessageRepository) : BaseApiController
{
    [HttpGet("get-room-messages/{roomId}")]
    public async Task<ActionResult<MessagesPageDto>> GetRoomMessages(
        ObjectId roomId,
        [FromQuery] MessageParams messageParams,
        CancellationToken cancellationToken
        )
    {
        string? hashedUserId = User.GetHashedUserId();

        if (hashedUserId is null)
            return Unauthorized("You are not logged in. Please login again.");

        ObjectId? userId = await tokenService.GetActualUserIdAsync(hashedUserId, cancellationToken);

        if (userId is null)
            return Unauthorized("You are not logged in. Please login again.");

        OperationResult<MessagesPageDto> opResult =
            await roomMessageRepository.GetAllMessagesAsync(roomId, userId.Value, messageParams, cancellationToken);

        return opResult.IsSuccess
            ? Ok(opResult.Result)
            : opResult.Error?.Code switch
            {
                ErrorCode.UserNotFound => BadRequest(opResult.Error.Message),
                ErrorCode.RoomNotFound => BadRequest(opResult.Error.Message),
                ErrorCode.NotRoomMember => BadRequest(opResult.Error.Message),
                _ => BadRequest("Operation failed! Try again later or contact support.")
            };
    }
}