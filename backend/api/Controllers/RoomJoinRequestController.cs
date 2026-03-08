using api.Controllers.Helpers;
using api.DTOs;
using api.DTOs.Helpers;
using api.Extensions;
using api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;

namespace api.Controllers;

public class RoomJoinRequestController(
    ITokenService tokenService,
    IRoomJoinRequestRepository roomJoinRequestRepository) : BaseApiController
{
    [HttpPost("join-request/{roomId}")]
    public async Task<ActionResult> JoinRequestRoom(ObjectId roomId, CancellationToken cancellationToken)
    {
        string? hashedUserId = User.GetHashedUserId();

        if (hashedUserId is null)
            return Unauthorized("You are not logged in. Please login again.");

        ObjectId? userId = await tokenService.GetActualUserIdAsync(hashedUserId, cancellationToken);

        if (userId is null)
            return Unauthorized("You are not logged in. Please login again.");

        OperationResult opResult =
            await roomJoinRequestRepository.JoinRequestAsync(roomId, userId.Value, cancellationToken);

        return opResult.IsSuccess
            ? Ok()
            : opResult.Error?.Code switch
            {
                ErrorCode.UserNotFound => BadRequest(opResult.Error.Message),
                ErrorCode.RoomNotFound => BadRequest(opResult.Error.Message),
                ErrorCode.JoinToOwnRoom => BadRequest(opResult.Error.Message),
                ErrorCode.PublicRoom => BadRequest(opResult.Error.Message),
                ErrorCode.AlreadyInRoom => BadRequest(opResult.Error.Message),
                ErrorCode.AlreadySentRequest => BadRequest(opResult.Error.Message),
                ErrorCode.UpdateFailed => BadRequest(opResult.Error.Message),
                _ => BadRequest("Operation failed! Try again later or contact support.")
            };
    }

    [Authorize(Policy = "RequiredOwnerRole")]
    [HttpPut("approve-request/{requestId}")]
    public async Task<ActionResult> ApproveRequest(ObjectId requestId, CancellationToken cancellationToken)
    {
        string? hashedUserId = User.GetHashedUserId();

        if (hashedUserId is null)
            return Unauthorized("You are not logged in. Please login again.");

        ObjectId? userId = await tokenService.GetActualUserIdAsync(hashedUserId, cancellationToken);

        if (userId is null)
            return Unauthorized("You are not logged in. Please login again.");

        OperationResult opResult =
            await roomJoinRequestRepository.ApproveRequestAsync(requestId, userId.Value, cancellationToken);

        return opResult.IsSuccess
            ? Ok()
            : opResult.Error?.Code switch
            {
                ErrorCode.UserNotFound => BadRequest(opResult.Error.Message),
                ErrorCode.MembershipPropNotExist => BadRequest(opResult.Error.Message),
                ErrorCode.AlreadyAccepted => BadRequest(opResult.Error.Message),
                ErrorCode.RoomNotFound => BadRequest(opResult.Error.Message),
                ErrorCode.NotRoomOwner => BadRequest(opResult.Error.Message),
                ErrorCode.TransactionFailed => BadRequest(opResult.Error.Message),
                _ => BadRequest("Operation failed! Try again later or contact support.")
            };
    }

    [Authorize(Policy = "RequiredOwnerRole")]
    [HttpPut("reject-request/{requestId}")]
    public async Task<ActionResult> RejectRequest(ObjectId requestId, CancellationToken cancellationToken)
    {
        string? hashedUserId = User.GetHashedUserId();

        if (hashedUserId is null)
            return Unauthorized("You are not logged in. Please login again.");

        ObjectId? userId = await tokenService.GetActualUserIdAsync(hashedUserId, cancellationToken);

        if (userId is null)
            return Unauthorized("You are not logged in. Please login again.");

        OperationResult opResult =
            await roomJoinRequestRepository.RejectRequestAsync(requestId, userId.Value, cancellationToken);

        return opResult.IsSuccess
            ? Ok()
            : opResult.Error?.Code switch
            {
                ErrorCode.MembershipPropNotExist => BadRequest(opResult.Error.Message),
                ErrorCode.AlreadyRejected => BadRequest(opResult.Error.Message),
                ErrorCode.RoomNotFound => BadRequest(opResult.Error.Message),
                ErrorCode.NotRoomOwner => BadRequest(opResult.Error.Message),
                ErrorCode.UpdateFailed => BadRequest(opResult.Error.Message),
                _ => BadRequest("Operation failed! Try again later or contact support.")
            };
    }
    
    [HttpGet("get-all-join-requests/{roomId}")]
    public async Task<ActionResult<IEnumerable<MembershipProposalResponse>>> GetAllJoinRequests(ObjectId roomId,
        CancellationToken cancellationToken)
    {
        string? hashedUserId = User.GetHashedUserId();
    
        if (hashedUserId is null)
            return Unauthorized("You are not logged in. Please login again.");
    
        ObjectId? userId = await tokenService.GetActualUserIdAsync(hashedUserId, cancellationToken);
    
        if (userId is null)
            return Unauthorized("You are not logged in. Please login again.");
    
        OperationResult<IEnumerable<MembershipProposalResponse>> opResult =
            await roomJoinRequestRepository.GetAllJoinRequestsAsync(roomId, userId.Value, cancellationToken);
    
        if (!opResult.Result.Any())
            return NoContent();
    
        return opResult.IsSuccess
            ? Ok(opResult.Result)
            : opResult.Error?.Code switch
            {
                ErrorCode.RoomNotFound => BadRequest(opResult.Error.Message),
                ErrorCode.NotRoomOwner => BadRequest(opResult.Error.Message),
                _ => BadRequest("Operation failed! Try again later or contact support.")
            };
    }
}