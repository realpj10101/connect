namespace api.DTOs;

public record UserDto(
    string Email,
    string UserName,
    DateTime CreatedAt
);