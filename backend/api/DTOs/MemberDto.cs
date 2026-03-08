namespace api.DTOs;

public record MemberDto(
    string UserName,
    string Email,
    bool IsOwner  
);