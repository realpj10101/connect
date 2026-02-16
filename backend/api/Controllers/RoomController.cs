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
public class RoomController(
    IRoomRepository roomRepository,
    ITokenService tokenService,
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

    [HttpGet]
    public async Task<ActionResult<RoomResponse>> GetAllRooms([FromQuery] PaginationParams paginationParams,
        CancellationToken cancellationToken)
    {
        string? hashedUserId = User.GetHashedUserId();

        if (hashedUserId is null)
            return Unauthorized("You are not logged in. Please login again.");

        ObjectId? userId = await tokenService.GetActualUserIdAsync(hashedUserId, cancellationToken);

        if (userId is null)
            return Unauthorized("You are not logged in. Please login again.");

        OperationResult<PagedList<Room>> opResult =
            await roomRepository.GetAllAsync(paginationParams, cancellationToken);

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
            
            roomRes.Add(Mappers.ConvertRoomToRoomResponse(room, userName));
        }

        return Ok(roomRes);
    }
}