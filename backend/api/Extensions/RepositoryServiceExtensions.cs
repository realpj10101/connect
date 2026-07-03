using api.Interfaces;
using api.Repositories;
using api.Services;
using Image_Processing_WwwRoot.Interfaces;
using Image_Processing_WwwRoot.Services;

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
        services.AddScoped<IAudioService, AudioService>();
        services.AddScoped<IAudioRepository, AudioRepository>();
        services.AddScoped<IFileClassifierService, FileClassifierService>();
        services.AddScoped<IVideoService, VideoService>();
        services.AddScoped<IPhotoService, PhotoService>();
        services.AddScoped<IPhotoModifySaveService, PhotoModifySaveService>();

        return services;
    }
}