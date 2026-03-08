using System.ComponentModel.DataAnnotations;
using api.Enums;

namespace api.DTOs;

public record CreateRoomDto(
    [Length(1, 30, ErrorMessage = "Room name must be between 1 and 30 characters.")]
    string RoomName,
    string RoomType
);

public record RoomResponse(
    string Id,
    string OwnerName,
    string RoomName,
    int MemberCount,
    RoomType RoomType,
    DateTime CreatedAt,
    bool IsMember,
    bool HasPendingRequest,
    bool IsOwner
);