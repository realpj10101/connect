using api.Controllers.Helpers;
using api.DTOs;
using api.DTOs.Helpers;
using api.Enums;
using api.Extensions;
using api.Helpers;
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
public class RoomMessageController(
    ITokenService tokenService,
    IRoomMessageRepository roomMessageRepository,
    IHubContext<RoomMessagingHub> _hub) : BaseApiController
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

    [HttpPost("upload-media/{roomId}")]
    public async Task<ActionResult<ChatItemDto>> UploadMedia(
        [AllowedMediaFileExtensions, FileSize(10 * 1024, 50 * 1024 * 1024)]
        IFormFile file, ObjectId roomId, CancellationToken cancellationToken
    )
    {
        string? hashedUserId = User.GetHashedUserId();

        if (hashedUserId is null)
            return Unauthorized("You are not logged in. Please login again.");

        ObjectId? userId = await tokenService.GetActualUserIdAsync(hashedUserId, cancellationToken);

        if (userId is null)
            return Unauthorized("You are not logged in. Please login again.");

        OperationResult<ChatItemDto> opResult =
            await roomMessageRepository.UploadMediaAsync(file, userId.Value, roomId, cancellationToken);

        if (!opResult.IsSuccess)
        {
            return opResult.Error!.Code switch
            {
                ErrorCode.UserNotFound => BadRequest(opResult.Error.Message),
                ErrorCode.RoomNotFound => BadRequest(opResult.Error.Message),
                ErrorCode.NotRoomMember => BadRequest(opResult.Error.Message),
                ErrorCode.SaveToDiskFailed => BadRequest(opResult.Error.Message),
                ErrorCode.InvalidFileType => BadRequest(opResult.Error.Message),
                _ => BadRequest("Operation failed! Try again later or contact support.")
            };
        }

        await _hub.Clients.Group(roomId.ToString())
            .SendAsync("RecieveMedia", opResult.Result, cancellationToken);

        return Ok(opResult.Result);
    }

    [HttpGet("stream/{messageId}")]
    public async Task<ActionResult> StreamMedia(
        ObjectId messageId,
        [FromQuery] string size = "enlarged",
        CancellationToken cancellationToken = default)
    {
        string? hashedUserId = User.GetHashedUserId();

        if (hashedUserId is null)
            return Unauthorized("You are not logged in. Please login again.");

        ObjectId? userId =
            await tokenService.GetActualUserIdAsync(hashedUserId, cancellationToken);

        if (userId is null)
            return Unauthorized("You are not logged in. Please login again.");

        OperationResult<RoomMessage> opResult =
            await roomMessageRepository.GetMessageByIdAsync(messageId, userId.Value, cancellationToken);

        if (!opResult.IsSuccess)
        {
            return opResult.Error?.Code switch
            {
                ErrorCode.MessageNotFound => BadRequest(opResult.Error.Message),
                ErrorCode.RoomNotFound => BadRequest(opResult.Error.Message),
                ErrorCode.NotRoomMember => BadRequest(opResult.Error.Message),
                _ => BadRequest("Operation failed! Try again later or contact support.")
            };
        }

        var message = opResult.Result;

        // ---------------------------------------------------------------
        // HANDLE IMAGE
        // ---------------------------------------------------------------
        if (message.Type == ChatItemType.Image)
        {
            if (message.Photo is null)
                return BadRequest("Photo URLs not found.");

            string? url = size.ToLower() switch
            {
                "165" or "small" or "nav" => message.Photo.Url_165,
                "256" or "medium" or "thumb" => message.Photo.Url_256,
                "enlarged" or "large" => message.Photo.Url_enlarged,
                _ => message.Photo.Url_enlarged
            };

            var filePath = Path.Combine("wwwroot", url);

            if (!System.IO.File.Exists(filePath))
                return NotFound("Image not found on server.");

            var mimeType = "image/jpeg";

            var stream = new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 32 * 1024,
                FileOptions.Asynchronous
            );
            
            return File(stream, mimeType, enableRangeProcessing: false);
        }
        
        // ---------------------------------------------------------------
        // HANDLE VIDEO
        // ---------------------------------------------------------------
        if (message.Type == ChatItemType.Video)
        {
            if (string.IsNullOrWhiteSpace(message.FilePath))
                return BadRequest("Video file path missing.");
            
            var filePath = Path.Combine("wwwroot", message.FilePath);
            
            if (!System.IO.File.Exists(filePath))
                return NotFound("Video file not found.");

            var mimeType = message.MimeType ?? "video/mp4";
            
            var stream = new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 64 * 1024,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
            
            return File(stream, mimeType, enableRangeProcessing: true);
        }
        
        return BadRequest("This message type cannot be streamed.");
    }
}