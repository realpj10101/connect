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
public class RoomMembershipController(
    ITokenService tokenService,
    IRoomMembershipRepository roomMembershipRepository) : BaseApiController
{
    [HttpPut("join/{roomId}")]
    public async Task<ActionResult<Response>> JoinRoom(ObjectId roomId, CancellationToken cancellationToken)
    {
        string? hashedUserId = User.GetHashedUserId();

        if (hashedUserId is null)
            return Unauthorized("You are not logged in. Please login again.");

        ObjectId? userId = await tokenService.GetActualUserIdAsync(hashedUserId, cancellationToken);

        if (userId is null)
            return Unauthorized("You are not logged in. Please login again.");

        OperationResult opResult =
            await roomMembershipRepository.JoinRoomAsync(roomId, userId.Value, cancellationToken);

        return opResult.IsSuccess
            ? Ok(new Response(
                Message: "You joined the room successfully."
            ))
            : opResult.Error?.Code switch
            {
                ErrorCode.UserNotFound => BadRequest(opResult.Error.Message),
                ErrorCode.RoomNotFound => BadRequest(opResult.Error.Message),
                ErrorCode.JoinToOwnRoom => BadRequest(opResult.Error.Message),
                ErrorCode.PrivateRoom => BadRequest(opResult.Error.Message),
                ErrorCode.AlreadyInRoom => BadRequest(opResult.Error.Message),
                ErrorCode.UpdateFailed => BadRequest(opResult.Error.Message),
                _ => BadRequest("Operation failed! Try again later or contact support.")
            };
    }

    [HttpPut("leave/{roomId}")]
    public async Task<ActionResult<Response>> LeaveRoom(ObjectId roomId, CancellationToken cancellationToken)
    {
        string? hashedUserId = User.GetHashedUserId();

        if (hashedUserId is null)
            return Unauthorized("You are not logged in. Please login again.");

        ObjectId? userId = await tokenService.GetActualUserIdAsync(hashedUserId, cancellationToken);

        if (userId is null)
            return Unauthorized("You are not logged in. Please login again.");

        OperationResult opResult =
            await roomMembershipRepository.LeaveRoomAsync(roomId, userId.Value, cancellationToken);

        return opResult.IsSuccess
            ? Ok(new Response(
                Message: "You left the room successfully."
            ))
            : opResult.Error?.Code switch
            {
                ErrorCode.UserNotFound => BadRequest(opResult.Error.Message),
                ErrorCode.RoomNotFound => BadRequest(opResult.Error.Message),
                ErrorCode.NotInRoom => BadRequest(opResult.Error.Message),
                ErrorCode.LeftOwnRoom => BadRequest(opResult.Error.Message),
                ErrorCode.UpdateFailed => BadRequest(opResult.Error.Message),
                _ => BadRequest("Operation failed! Try again later or contact support.")
            };
    }

    [Authorize(Policy = "RequiredOwnerRole")]
    [HttpPut("remove-member/{roomId}/{targetUserName}")]
    public async Task<ActionResult<Response>> RemoveMember(ObjectId roomId, string targetUserName,
        CancellationToken cancellationToken)
    {
        string? hashedUserId = User.GetHashedUserId();

        if (hashedUserId is null)
            return Unauthorized("You are not logged in. Please login again.");

        ObjectId? userId = await tokenService.GetActualUserIdAsync(hashedUserId, cancellationToken);

        if (userId is null)
            return Unauthorized("You are not logged in. Please login again.");

        OperationResult opResult =
            await roomMembershipRepository.RemoveMemberAsync(roomId, userId.Value, targetUserName, cancellationToken);

        return opResult.IsSuccess
            ? Ok(new Response(Message: "Member successfully removed."))
            : opResult.Error?.Code switch
            {
                ErrorCode.RoomNotFound => BadRequest(opResult.Error.Message),
                ErrorCode.NotRoomOwner => BadRequest(opResult.Error.Message),
                ErrorCode.UserNotFound => BadRequest(opResult.Error.Message),
                ErrorCode.LeftOwnRoom => BadRequest(opResult.Error.Message),
                ErrorCode.NotInRoom => BadRequest(opResult.Error.Message),
                ErrorCode.UpdateFailed => BadRequest(opResult.Error.Message),
                _ => BadRequest("Operation failed! Try again later or contact support.")
            };
    }

    [HttpGet("get-room-members/{roomId}")]
    public async Task<ActionResult<IEnumerable<MemberDto>>> GetRoomMembers(ObjectId roomId,
        CancellationToken cancellationToken)
    {
        OperationResult<IEnumerable<MemberDto>> opResult =
            await roomMembershipRepository.GetRoomMembersAsync(roomId, cancellationToken);

        if (!opResult.IsSuccess)
        {
            return opResult.Error?.Code switch
            {
                ErrorCode.RoomNotFound => BadRequest(opResult.Error.Message),
                _ => BadRequest("Operation failed! Try again later or contact support.")
            };
        }

        if (!opResult.Result.Any())
            return NoContent();

        return Ok(opResult.Result);
    }

    [HttpGet("get-rooms-user-is-member")]
    public async Task<ActionResult<IEnumerable<RoomResponse>>> GetRoomsUserIsMemberOf(
        CancellationToken cancellationToken)
    {
        string? hashedUserId = User.GetHashedUserId();
        
        if (hashedUserId is null)
            return Unauthorized("You are not logged in. Please login again.");
        
        ObjectId? userId = await tokenService.GetActualUserIdAsync(hashedUserId, cancellationToken);
        
        if (userId is null)
            return Unauthorized("You are not logged in. Please login again.");

        OperationResult<IEnumerable<RoomResponse>> opResult =
            await roomMembershipRepository.GetRoomsUserIsMemberOfAsync(userId.Value, cancellationToken);
        
        if (!opResult.Result.Any())
            return NoContent();
        
        return Ok(opResult.Result);
    }
}