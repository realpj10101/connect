using api.DTOs;
using api.DTOs.Helpers;

namespace api.Interfaces;

public interface IAccountRepository
{
    public Task<OperationResult<LoggedInDto>> RegisterAsync(RegisterDto request, CancellationToken cancellationToken);
    public Task<OperationResult<LoggedInDto>> LoginAsync(LoginDto request, CancellationToken cancellationToken);
    public Task<OperationResult<LoggedInDto>> ReloadLoggedInUserAsync(string hashedUserId, string token, CancellationToken cancellationToken);
    // public Task<OperationResult> UpdateLastActiveAsync(string loggedInUserId, CancellationToken cancellationToken);
}