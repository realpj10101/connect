using System.Security.Claims;

namespace api.Extensions;

public static class ClaimPrincipalExtensions
{  
    public static string? GetHashedUserId(this ClaimsPrincipal user)
    {
        return user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    public static string? GetUserName(this ClaimsPrincipal user)
    {
        return user.FindFirst(ClaimTypes.Name)?.Value;
    }
}