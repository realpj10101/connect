using api.Enums;
using api.Models;

namespace api.DTOs;

public static class Mappers
{
    public static AppUser ConvertRegisterDtoToAppUser(RegisterDto registerDto) =>
        new()
        {
            Email = registerDto.Email,
            UserName = registerDto.UserName
        };

    public static LoggedInDto ConvertAppUserToLoggedInDto(AppUser appUser, string token) =>
        new()
        {
            Token = token,
            UserName = appUser.UserName
        };

    public static RoomResponse ConvertRoomToRoomResponse(Room room, string userName, bool isMember,
        bool hasPendingRequest, bool isOwner) =>
        new(
            room.Id.ToString()!,
            userName,
            room.Name,
            room.MembersCount,
            room.RoomType,
            room.CreatedAt,
            isMember,
            hasPendingRequest,
            isOwner
        );

    public static ChatItemDto ConvertAudioToChatItemDto(RoomMessage audio, string userName, ChatItemType chatItemType) =>
        new(
            audio.Id.ToString()!,
            chatItemType,
            userName,
            null,
            audio.Duration,
            audio.FileSize,
            audio.TimeStamp
        );
}