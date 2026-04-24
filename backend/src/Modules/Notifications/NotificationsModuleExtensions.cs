using IMS.Modular.Modules.Notifications.Api;
using IMS.Modular.Modules.Notifications.Application;
using IMS.Modular.Modules.Notifications.Application.Commands;
using IMS.Modular.Modules.Notifications.Infrastructure;
using IMS.Modular.Shared.Database;
using Microsoft.EntityFrameworkCore;

namespace IMS.Modular.Modules.Notifications;

/// <summary>
/// US-066: DI registration para o módulo de Notificações.
/// </summary>
public static class NotificationsModuleExtensions
{
    public static IServiceCollection AddNotificationsModule(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        services.AddDbContext<NotificationsDbContext>((sp, options) =>
        {
            var env = sp.GetRequiredService<IWebHostEnvironment>();
            options.UseImsDatabase(configuration, env);
        });

        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<SendNotificationHandler>();

        return services;
    }

    public static async Task InitializeNotificationsModuleAsync(this IServiceProvider services)
        => await services.ApplyMigrationsAsync<NotificationsDbContext>();

    public static IEndpointRouteBuilder MapNotificationsModule(this IEndpointRouteBuilder endpoints)
        => NotificationsModule.Map(endpoints);
}
