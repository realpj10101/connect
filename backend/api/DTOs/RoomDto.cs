namespace api.DTOs;

public record CreateRoomDto(
    string Name,
    string RoomType
);

public record RoomResponse(
    string Id,
    string OwnerName,
    string Name,
    int MemberCount,
    DateTime CreatedAt
);