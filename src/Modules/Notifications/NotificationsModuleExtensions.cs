using IMS.Modular.Modules.Notifications.Application;
using IMS.Modular.Modules.Notifications.Infrastructure;

namespace IMS.Modular.Modules.Notifications;

public static class NotificationsModuleExtensions
{
    public static IServiceCollection AddNotificationsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // SignalR
        services.AddSignalR();

        // Email settings
        var emailSettings = new EmailSettings();
        configuration.GetSection("Email").Bind(emailSettings);
        services.AddSingleton(emailSettings);

        // Services
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddSingleton<IMessageBusService, MessageBusService>();

        return services;
    }

    public static IEndpointRouteBuilder MapNotificationsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // SignalR Hub
        endpoints.MapHub<NotificationHub>("/hubs/notifications")
            .RequireAuthorization();

        return endpoints;
    }
}
