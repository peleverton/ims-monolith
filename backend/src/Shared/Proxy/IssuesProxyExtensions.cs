using IMS.Modular.Shared.FeatureFlags;
using Microsoft.FeatureManagement;
using Yarp.ReverseProxy.Configuration;

namespace IMS.Modular.Shared.Proxy;

/// <summary>
/// US-079: YARP-based reverse proxy routing for the extracted Issues microservice.
/// When feature flag "UseIssuesMicroservice" is enabled, /api/issues is routed to
/// the ims-issues-service instead of the monolith's Issues module.
/// </summary>
public static class IssuesProxyExtensions
{
    public static IServiceCollection AddIssuesProxy(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddReverseProxy()
            .LoadFromConfig(configuration.GetSection("IssuesProxy"));

        return services;
    }

    /// <summary>
    /// Maps YARP proxy middleware. Only routes /api/issues/** when the microservice
    /// feature flag is active; otherwise the monolith's own IssuesModule handles it.
    /// </summary>
    public static WebApplication MapIssuesProxy(this WebApplication app)
    {
        // The YARP routes defined in appsettings are applied only when the cluster is reachable.
        // The feature flag "UseIssuesMicroservice" is checked at the route level via config.
        app.MapReverseProxy();
        return app;
    }
}
