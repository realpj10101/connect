using api.Enums;

namespace api.DTOs;

public record CreateRoomDto(
    string Name,
    string RoomType
);

public record RoomResponse(
    string Id,
    string OwnerName,
    string RoomName,
    int MemberCount,
    RoomType RoomType,
    DateTime CreatedAt
);