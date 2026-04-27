using IMS.Modular.Shared.FeatureFlags;
using Microsoft.FeatureManagement;

namespace IMS.Modular.Modules.Features.Api;

/// <summary>
/// US-074: GET /api/features — returns current state of feature flags for the frontend.
/// </summary>
public static class FeaturesModule
{
    public static void Map(WebApplication app)
    {
        app.MapGet("/api/features", async (IFeatureManager featureManager) =>
        {
            var flags = new Dictionary<string, bool>
            {
                [FeatureFlags.EnableKanbanView]     = await featureManager.IsEnabledAsync(FeatureFlags.EnableKanbanView),
                [FeatureFlags.EnableWebhooks]       = await featureManager.IsEnabledAsync(FeatureFlags.EnableWebhooks),
                [FeatureFlags.EnableFullTextSearch] = await featureManager.IsEnabledAsync(FeatureFlags.EnableFullTextSearch),
            };

            return Results.Ok(flags);
        })
        .WithName("GetFeatureFlags")
        .WithTags("Features")
        .AllowAnonymous()
        .Produces<Dictionary<string, bool>>(200);
    }
}
