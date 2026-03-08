using System.Net;
using api.Controllers.Helpers;
using api.DTOs;
using api.DTOs.Helpers;
using api.Extensions;
using api.Helpers;
using api.Interfaces;
using api.Models;
using api.Models.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;

namespace api.Controllers;

[Authorize]
public class RoomManagementController(
    IRoomManagementRepository roomManagementRepository,
    ITokenService tokenService,
    IRoomMembershipRepository roomMembershipRepository,
    IRoomJoinRequestRepository roomJoinRequestRepository,
    IUserRepository userRepository) : BaseApiController
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

        OperationResult opResult =
            await roomManagementRepository.CreateRoomAsync(request, userId.Value, cancellationToken);

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

    [HttpGet]
    public async Task<ActionResult<RoomResponse>> GetAllRooms([FromQuery] RoomParams roomParams,
        CancellationToken cancellationToken)
    {
        string? hashedUserId = User.GetHashedUserId();

        if (hashedUserId is null)
            return Unauthorized("You are not logged in. Please login again.");

        ObjectId? userId = await tokenService.GetActualUserIdAsync(hashedUserId, cancellationToken);

        if (userId is null)
            return Unauthorized("You are not logged in. Please login again.");

        roomParams.UserId = userId.Value;

        OperationResult<PagedList<Room>> opResult =
            await roomManagementRepository.GetAllAsync(roomParams, cancellationToken);

        if (opResult.Result.Count == 0)
            return NoContent();

        PaginationHeader paginationHeader = new(
            CurrentPage: opResult.Result.CurrentPage,
            ItemsPerPage: opResult.Result.PageSize,
            TotalItems: opResult.Result.TotalItems,
            TotalPages: opResult.Result.TotalPages
        );

        Response.AddPaginationHeader(paginationHeader);

        List<RoomResponse> roomRes = [];

        foreach (Room room in opResult.Result)
        {
            string? userName;

            userName = await userRepository.GetUserNameByIdAsync(room.OwnerId, cancellationToken);

            if (userName is null)
                userName = string.Empty;

            bool isMember =
                await roomMembershipRepository.CheckIsMemberAsync(userId.Value, room.Id!.Value, cancellationToken);
            bool hasPendingRequest =
                await roomJoinRequestRepository.CheckHasPendingRequestsAsync(userId.Value, room.Id!.Value,
                    cancellationToken);
            bool isOwner =
                await roomManagementRepository.CheckIsOwnRoomAsync(userId.Value, room.Id!.Value, cancellationToken);

            roomRes.Add(Mappers.ConvertRoomToRoomResponse(room, userName, isMember, hasPendingRequest, isOwner));
        }

        return Ok(roomRes);
    }

    [HttpGet("get-room-by-id/{roomId}")]
    public async Task<ActionResult<RoomResponse>> GetRoomById(ObjectId roomId, CancellationToken cancellationToken)
    {
        string? hashedUserId = User.GetHashedUserId();

        if (hashedUserId is null)
            return Unauthorized("You are not logged in. Please login again.");

        ObjectId? userId = await tokenService.GetActualUserIdAsync(hashedUserId, cancellationToken);

        if (userId is null)
            return Unauthorized("You are not logged in. Please login again.");

        OperationResult<RoomResponse> opResult =
            await roomManagementRepository.GetRoomByIdAsync(roomId, userId.Value, cancellationToken);

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

    [HttpPut("update-room/{roomId}")]
    public async Task<ActionResult<Response>> UpdateRoom(ObjectId roomId, RoomUpdateDto req,
        CancellationToken cancellationToken)
    {
        string? hashedUserId = User.GetHashedUserId();

        if (hashedUserId is null)
            return Unauthorized("You are not logged in. Please login again.");

        ObjectId? userId = await tokenService.GetActualUserIdAsync(hashedUserId, cancellationToken);

        if (userId is null)
            return Unauthorized("You are not logged in. Please login again.");

        OperationResult opResult =
            await roomManagementRepository.UpdateRoomAsync(roomId, userId.Value, req, cancellationToken);

        return opResult.IsSuccess
            ? Ok(new Response(Message: "Room successfully updated."))
            : opResult.Error?.Code switch
            {
                ErrorCode.RoomNotFound => BadRequest(opResult.Error.Message),
                ErrorCode.NotRoomOwner => BadRequest(opResult.Error.Message),
                ErrorCode.UpdateFailed => BadRequest(opResult.Error.Message),
                _ => BadRequest("Operation failed! Try again later or contact support.")
            };
    }

    [HttpGet("get-rooms-created-by-user")]
    public async Task<ActionResult<RoomResponse>> GetRoomsCreatedByUserAsync(CancellationToken cancellationToken)
    {
        string? hashedUserId = User.GetHashedUserId();
        string? userName = User.GetUserName();

        if (hashedUserId is null || userName is null)
            return Unauthorized("You are not logged in. Please login again.");

        ObjectId? userId = await tokenService.GetActualUserIdAsync(hashedUserId, cancellationToken);

        if (userId is null)
            return Unauthorized("You are not logged in. Please login again.");

        OperationResult<IEnumerable<RoomResponse>> opResult =
            await roomManagementRepository.GetRoomsCreatedByUserAsync(userId.Value, userName, cancellationToken);

        if (!opResult.Result.Any())
            return NoContent();

        return Ok(opResult.Result);
    }
}