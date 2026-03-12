using api.Enums;

namespace api.DTOs;

public record AudioResponseDto(
    string Id,
    string RoomId,
    string UploaderUserName,
    int Duration,
    long FileSize,
    AudioType Type,
    DateTime CreatedAt
);