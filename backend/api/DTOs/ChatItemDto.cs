using api.Enums;

namespace api.DTOs;

public record ChatItemDto(
    string Id,
    ChatItemType Type,
    string SenderUserName,
    string? Message,
    int? Duration,
    long? FileSize,
    DateTime CreatedAt
);