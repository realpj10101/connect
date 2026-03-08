using api.Interfaces;
using api.Repositories;
using api.Services;

namespace api.Extensions;

public static class RepositoryServiceExtensions
{
    public static IServiceCollection AddRepositoryServices(this IServiceCollection services)
    {
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoomManagementRepository, RoomManagementRepository>();
        services.AddScoped<IRoomMembershipRepository, RoomMembershipRepository>();
        services.AddScoped<IRoomJoinRequestRepository, RoomJoinRequestRepository>();
        services.AddScoped<IRoomMessageRepository, RoomMessageRepository>();

        return services;
    }
}