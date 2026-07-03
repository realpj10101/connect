using api.Controllers.Helpers;
using api.DTOs;
using api.DTOs.Helpers;
using api.Enums;
using api.Extensions;
using api.Interfaces;
using api.Models;
using api.SignalR;
using api.Validations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Bson;

namespace api.Controllers;

[Authorize]
public class AudioController(
    ITokenService tokenService,
    IAudioRepository audioRepository,
    IUserRepository userRepository,
    IRoomMessageRepository roomMessageRepository,
    IHubContext<RoomMessagingHub> _hub) : BaseApiController
{
    [HttpPost("upload-voice/{roomId}")]
    public async Task<ActionResult<ChatItemDto>> UploadVoice(
        [FromForm, AllowedFileExtensions, FileSize(2 * 1024, 5 * 1024 * 1024)]
        IFormFile voiceFile, ObjectId roomId, CancellationToken cancellationToken)
    {
        string? hashedUserId = User.GetHashedUserId();

        if (hashedUserId is null)
            return Unauthorized("You are not logged in. Please login again.");

        ObjectId? userId = await tokenService.GetActualUserIdAsync(hashedUserId, cancellationToken);

        if (userId is null)
            return Unauthorized("You are not logged in. Please login again.");

        OperationResult<RoomMessage> opResult =
            await audioRepository.UploadVoiceAsync(voiceFile, roomId, userId.Value, cancellationToken);

        if (!opResult.IsSuccess)
        {
            return opResult.Error?.Code switch
            {
                ErrorCode.RoomNotFound => NotFound(opResult.Error.Message),
                ErrorCode.NotRoomMember => BadRequest(opResult.Error.Message),
                ErrorCode.SaveToDiskFailed => BadRequest(opResult.Error.Message),
                _ => BadRequest("Operation failed! Try again later or contact support.")
            };
        }

        string? userName = await userRepository.GetUserNameByIdAsync(opResult.Result.SenderId, cancellationToken);

        if (userName is null)
            userName = "Unknown";

        ChatItemDto audioResponseDto = Mappers.ConvertRoomMessageToChatItemDto(opResult.Result, userName, ChatItemType.Voice);

        await _hub.Clients.Group(roomId.ToString())
            .SendAsync("ReceiveVoice", audioResponseDto, cancellationToken);

        return Ok(audioResponseDto);
    }

    [HttpPost("upload-audio/{roomId}")]
    public async Task<ActionResult<AudioResponseDto>> UploadAudio(
        [FromForm, AllowedFileExtensions, FileSize(250 * 1024, 40 * 1024 * 1024)]
        IFormFile audioFile, ObjectId roomId, CancellationToken cancellationToken
    )
    {
        string? hashedUserId = User.GetHashedUserId();

        if (hashedUserId is null)
            return Unauthorized("You are not logged in. Please login again.");

        ObjectId? userId = await tokenService.GetActualUserIdAsync(hashedUserId, cancellationToken);

        if (userId is null)
            return Unauthorized("You are not logged in. Please login again.");
    
        OperationResult<RoomMessage> opResult = await audioRepository.UploadAudioAsync(audioFile, roomId, userId.Value, cancellationToken);
        
        if (!opResult.IsSuccess)
        {
            return opResult.Error?.Code switch
            {
                ErrorCode.RoomNotFound => NotFound(opResult.Error.Message),
                ErrorCode.NotRoomMember => BadRequest(opResult.Error.Message),
                ErrorCode.SaveToDiskFailed => BadRequest(opResult.Error.Message),
                _ => BadRequest("Operation failed! Try again later or contact support.")
            };
        }
        
        string? userName = await userRepository.GetUserNameByIdAsync(opResult.Result.SenderId, cancellationToken);

        if (userName is null)
            userName = "Unknown";

        ChatItemDto audioResponseDto = Mappers.ConvertRoomMessageToChatItemDto(opResult.Result, userName, ChatItemType.Audio);
        
        await _hub.Clients.Group(roomId.ToString())
            .SendAsync("ReceiveAudio", audioResponseDto, cancellationToken);

        return Ok(audioResponseDto);
    }

    [HttpGet("stream/{audioId}")]
    public async Task<ActionResult> StreamAudio(ObjectId audioId, CancellationToken cancellationToken)
    {
        string? hashedUserId = User.GetHashedUserId();

        if (hashedUserId is null)
            return Unauthorized("You are not logged in. Please login again.");

        ObjectId? userId = await tokenService.GetActualUserIdAsync(hashedUserId, cancellationToken);

        if (userId is null)
            return Unauthorized("You are not logged in. Please login again.");

        OperationResult<RoomMessage> opResult =
            await roomMessageRepository.GetMessageByIdAsync(audioId, userId.Value, cancellationToken);

        if (!opResult.IsSuccess)
        {
            return opResult.Error?.Code switch
            {
                ErrorCode.AudioNotFound => NotFound(opResult.Error.Message),
                ErrorCode.RoomNotFound => BadRequest(opResult.Error.Message),
                ErrorCode.NotRoomMember => BadRequest(opResult.Error.Message),
                _ => BadRequest("Operation failed! Try again later or contact support.")
            };
        }

        var path = Path.Combine("wwwroot", opResult.Result.FilePath!);

        if (!System.IO.File.Exists(path))
            return NotFound("Audio file not found on server.");

        var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            4096,
            FileOptions.Asynchronous);

        return File(stream, opResult.Result.MimeType!, enableRangeProcessing: true);
    }
}