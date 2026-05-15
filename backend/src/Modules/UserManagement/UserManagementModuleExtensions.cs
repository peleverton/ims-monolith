using IMS.Modular.Modules.UserManagement.Application.Validators;
using IMS.Modular.Modules.UserManagement.Infrastructure;
using IMS.Modular.Shared.Abstractions;
using IMS.Modular.Shared.Email;
using FluentValidation;

namespace IMS.Modular.Modules.UserManagement;

/// <summary>
/// US-064: DI registration for the UserManagement module.
/// Note: no separate DbContext — reads/writes via AuthDbContext (shared schema).
/// </summary>
public static class UserManagementModuleExtensions
{
    public static IServiceCollection AddUserManagementModule(this IServiceCollection services)
    {
        // Repository — wraps AuthDbContext, registered by AddAuthModule
        services.AddScoped<IUserManagementRepository, UserManagementRepository>();

        // US-089: LGPD/GDPR repository (crosses module DB boundaries by design)
        services.AddScoped<IGdprRepository, GdprRepository>();

        // US-089: Email service — log-based for dev/test; swap for SMTP in production
        services.AddScoped<IEmailService, LogEmailService>();

        // Validators
        services.AddValidatorsFromAssemblyContaining<UpdateProfileRequestValidator>();

        return services;
    }
}

