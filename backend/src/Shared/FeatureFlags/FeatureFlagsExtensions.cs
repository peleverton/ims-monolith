using Microsoft.FeatureManagement;

namespace IMS.Modular.Shared.FeatureFlags;

/// <summary>
/// US-074: Extension methods to register feature management.
/// </summary>
public static class FeatureFlagsExtensions
{
    public static IServiceCollection AddImsFeatureFlags(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddFeatureManagement(configuration.GetSection("FeatureManagement"));
        return services;
    }
}
