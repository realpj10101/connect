using System.Text.RegularExpressions;
using api.DTOs;
using api.DTOs.Helpers;
using api.Extensions;
using api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Bson;

namespace api.SignalR;

[Authorize]
public class RoomMessagingHub : Hub
{
    private IRoomManagementRepository _roomManagementRepository;
    private IRoomMessageRepository _roomMessageRepository;
    private IRoomMembershipRepository _roomMembershipRepository;
    private ITokenService _tokenService;

    public RoomMessagingHub(IRoomManagementRepository roomManagementRepository, ITokenService tokenService, IRoomMessageRepository roomMessageRepository, IRoomMembershipRepository roomMembershipRepository)
    {
        _roomManagementRepository = roomManagementRepository;
        _tokenService = tokenService;
        _roomMessageRepository = roomMessageRepository;
        _roomMembershipRepository = roomMembershipRepository;
    }

    public async Task SendMessageAsync(MessageRequest req, string roomId)
    {
        CancellationToken cancellationToken = Context.ConnectionAborted;

        if (!ObjectId.TryParse(roomId, out var parsedRoomId))
            throw new HubException("Invalid room id.");
        
        string? hashedUserId = Context?.User?.GetHashedUserId();

        if (hashedUserId is null)
        {
            throw new HubException("User is not authenticated.");
        }

        ObjectId? userId = await _tokenService.GetActualUserIdAsync(hashedUserId, cancellationToken);

        if (userId is null)
        {
            throw new HubException("User ID could not be retrieved.");
        }

        OperationResult<MessageResponseDto> res =
            await _roomMessageRepository.SavedMessageAsync(req, userId.Value, parsedRoomId, cancellationToken);

        if (!res.IsSuccess)
        {
            throw new HubException(res.Error?.Message ?? "Message saving failed.");
        }

        await Clients.Group(roomId)
            .SendAsync("ReceiveMessage", res.Result, cancellationToken);
    }

    public async Task JoinRoom(string roomId)
    {
        CancellationToken cancellationToken = Context.ConnectionAborted;

        if (!ObjectId.TryParse(roomId, out var parsedRoomId))
            throw new HubException("Invalid room id.");

        string? hashedUserId = Context?.User?.GetHashedUserId();
        
        if (hashedUserId is null)
            throw new HubException("User is not authenticated.");

        ObjectId? userId = await _tokenService.GetActualUserIdAsync(hashedUserId, cancellationToken);

        if (userId is null)
            throw new HubException("User ID could not be retrieved.");

        bool hasAccess =
            await _roomMembershipRepository.CheckMembershipAsync(parsedRoomId, userId.Value, cancellationToken);
        
        if (!hasAccess)
            throw new HubException("You are not a member of this room. Please join the room first.");
    
        await Groups.AddToGroupAsync(Context.ConnectionId, roomId.ToString(), cancellationToken);

        // OperationResult<IEnumerable<MessageResponseDto>> opResult =
        //     await _roomMessageRepository.GetAllMessagesAsync(parsedRoomId, userId.Value, cancellationToken);
        //
        // if (!opResult.IsSuccess)
        //     throw new HubException(opResult.Error?.Message ?? "Failed to load messages.");
        //
        // await Clients.Caller.SendAsync(
        //     "LoadMessages",
        //     opResult.Result,
        //     cancellationToken
        // );

        string? username = Context.User.GetUserName();

        await Clients.OthersInGroup(roomId)
            .SendAsync("UserJoined", username, cancellationToken);
    }

    public async Task LeaveRoom(string roomId)
    {
        CancellationToken cancellationToken = Context.ConnectionAborted;
        
        string? hashedUserId = Context?.User?.GetHashedUserId();

        if (hashedUserId is null)
            throw new HubException("User is not authenticated.");

        ObjectId? userId = await _tokenService.GetActualUserIdAsync(hashedUserId, cancellationToken);

        if (userId is null)
            throw new HubException("User ID could not be retrieved.");
        
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId, cancellationToken);
        
        string? username = Context.User.GetUserName();

        await Clients.OthersInGroup(roomId)
            .SendAsync("UserLeft", username, cancellationToken);
    }

    public async Task StartTyping(string roomId)
    {
        string? userName = Context.User.GetUserName() ?? "Unknown";
        
        await Clients.OthersInGroup(roomId)
            .SendAsync("UserTyping", userName);
    }

    public async Task StopTyping(string roomId)
    {
        string? userName = Context.User.GetUserName() ?? "Unknown";
        
        await Clients.OthersInGroup(roomId)
            .SendAsync("UserStoppedTyping", userName);
    }
}