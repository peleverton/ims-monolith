using IMS.Modular.Modules.UserManagement.Application.Validators;
using IMS.Modular.Modules.UserManagement.Infrastructure;
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

        // Validators
        services.AddValidatorsFromAssemblyContaining<UpdateProfileRequestValidator>();

        return services;
    }
}
