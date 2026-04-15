using IMS.Modular.Shared.Abstractions;
using IMS.Modular.Shared.Errors;

namespace IMS.Modular.Shared.Middleware;

/// <summary>
/// Extension methods for registering middleware services and configuring the middleware pipeline.
/// </summary>
public static class MiddlewareExtensions
{
    /// <summary>
    /// Registers middleware-related services in DI (IUserContext, ICorrelationIdAccessor, IHttpContextAccessor).
    /// Call in Program.cs during service registration.
    /// </summary>
    public static IServiceCollection AddMiddlewareServices(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<UserContext>();
        services.AddScoped<IUserContext>(sp => sp.GetRequiredService<UserContext>());
        services.AddSingleton<ICorrelationIdAccessor, CorrelationIdAccessor>();

        return services;
    }

    /// <summary>
    /// Configures the middleware pipeline in the correct order.
    /// Call in Program.cs after Build() and before UseAuthentication().
    ///
    /// Pipeline order:
    ///   ExceptionHandling → CorrelationId → Metrics → PerformanceTiming → [Auth] → UserContext → [Routing]
    /// </summary>
    public static WebApplication UseImsMiddleware(this WebApplication app)
    {
        // 0. ExceptionHandling — outermost, catches all unhandled exceptions → ProblemDetails
        app.UseMiddleware<ExceptionHandlingMiddleware>();

        // 1. CorrelationId — early, so all downstream middleware/logs have the ID
        app.UseMiddleware<CorrelationIdMiddleware>();

        // 2. Metrics — wraps everything to capture full request lifecycle
        app.UseMiddleware<MetricsMiddleware>();

        // 3. Performance — warns on slow requests
        app.UseMiddleware<PerformanceTimingMiddleware>();

        return app;
    }

    /// <summary>
    /// Adds UserContext middleware. Must be called AFTER UseAuthentication() + UseAuthorization().
    /// </summary>
    public static WebApplication UseUserContext(this WebApplication app)
    {
        app.UseMiddleware<UserContextMiddleware>();
        return app;
    }
}
