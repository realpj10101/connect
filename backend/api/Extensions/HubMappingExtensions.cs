using api.SignalR;

namespace api.Extensions;

public static class HubMappingExtensions
{
    public static void MapHubs(this WebApplication app)
    {
        app.MapHub<ChatHub>("hubs/chat");
    }
}