namespace api.DTOs;

public record MessageRequest(
    string Message
);

public record MessageResponseDto(
    string Id,
    string Message,
    string SenderUserName,
    DateTime TimeStamp
);

public record MessagesPageDto(
    List<ChatItemDto> Messages,
    bool HasMore
);