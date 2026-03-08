using System.ComponentModel.DataAnnotations;
using api.Enums;

namespace api.DTOs;

public record RoomUpdateDto(
    [Length(1, 30, ErrorMessage = "Room name must be between 1 and 30 characters.")]
    string RoomName,
    [EnumDataType(typeof(RoomType), ErrorMessage = "Invalid room type. Allowed values: Public, Private.")]
    RoomType RoomType
);