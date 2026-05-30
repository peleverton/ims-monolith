using IMS.Modular.Modules.UserManagement;
using IMS.Modular.Modules.UserManagement.Api;
using IMS.Modular.Shared.FeatureFlags;
using IMS.Modular.Shared.MultiTenancy.TenantManagement;
using FluentValidation;
using IMS.Modular.Modules.Analytics;
using IMS.Modular.Modules.Analytics.Api;
using IMS.Modular.Modules.Audit;
using IMS.Modular.Modules.Auth;
using IMS.Modular.Modules.Auth.Api;
using IMS.Modular.Modules.Billing;
using IMS.Modular.Modules.Billing.Api;
using IMS.Modular.Modules.Billing.Application.Behaviors;
using IMS.Modular.Modules.SmartAssigner;
using IMS.Modular.Modules.SmartAssigner.Api;
using IMS.Modular.Modules.WarehouseRouting;
using IMS.Modular.Modules.WarehouseRouting.Api;
using IMS.Modular.Modules.Features.Api;
using IMS.Modular.Modules.Search;
using IMS.Modular.Modules.Search.Api;
using IMS.Modular.Modules.Inventory;
using IMS.Modular.Modules.Inventory.Api;
using IMS.Modular.Modules.InventoryIssues;
using IMS.Modular.Modules.InventoryIssues.Api;
using IMS.Modular.Modules.Issues;
using IMS.Modular.Modules.Issues.Api;
using IMS.Modular.Modules.Jobs;
using IMS.Modular.Modules.Notifications;
using IMS.Modular.Modules.Notifications.Api;
using IMS.Modular.Modules.Webhooks;
using IMS.Modular.Shared.Abstractions;
using IMS.Modular.Shared.Behaviors;
using IMS.Modular.Shared.Caching;
using IMS.Modular.Shared.HealthChecks;
using IMS.Modular.Shared.Middleware;
using IMS.Modular.Shared.Observability;
using IMS.Modular.Shared.Messaging;
using IMS.Modular.Shared.Outbox;
using IMS.Modular.Shared.Proxy;
using IMS.Modular.Shared.RateLimiting;
using IMS.Modular.Shared.MultiTenancy;
using MediatR;
using Microsoft.FeatureManagement;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Text.Json.Serialization;

// ============================================================
// BOOTSTRAP LOGGER (captures startup errors before DI is ready)
// ============================================================
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// SERILOG (Structured Logging — US-006)
// ============================================================

builder.AddSerilogLogging();

// ============================================================
// SHARED SERVICES
// ============================================================

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "IMS Modular API",
        Version = "v1",
        Description = "Issue Management System — Modular Monolith with Minimal API, CQRS, and Clean Architecture",
        Contact = new OpenApiContact { Name = "IMS Team", Email = "support@ims.local" }
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header. Enter 'Bearer' [space] and then your token.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// JSON options
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

// MediatR (scans all handlers in current assembly)
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);

    // Pipeline Behaviors (order matters: Validation → Quota → Logging → Caching → Handler)
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(QuotaBehavior<,>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(CachingBehavior<,>));
});

// FluentValidation (scans all validators in current assembly)
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

// ============================================================
// CACHING (US-008: Output Caching + Redis Distributed Cache)
// ============================================================

builder.Services.AddImsCaching(builder.Configuration);

// Middleware services (IUserContext, ICorrelationIdAccessor, IHttpContextAccessor)
builder.Services.AddMiddlewareServices();

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

// ============================================================
// MESSAGING (US-023: RabbitMQ Message Bus)
// ============================================================

builder.Services.AddImsMessaging(builder.Configuration);

// ============================================================
// OUTBOX (US-023: Outbox pattern — reliable event publishing)
// ============================================================

builder.Services.AddImsOutbox(builder.Configuration);

// ============================================================
// MODULE REGISTRATION
// ============================================================

builder.Services.AddAuthModule(builder.Configuration);
// US-064: UserManagement module — must come after AddAuthModule (shares AuthDbContext)
builder.Services.AddUserManagementModule();
builder.Services.AddIssuesModule(builder.Configuration);
builder.Services.AddInventoryModule(builder.Configuration);
builder.Services.AddInventoryIssuesModule(builder.Configuration);
builder.Services.AddAnalyticsModule(builder.Configuration);

// US-066: Notifications Module
builder.Services.AddNotificationsModule(builder.Configuration, builder.Environment);

// US-069: Webhooks Module
builder.Services.AddWebhooksModule(builder.Configuration, builder.Environment);

// US-074: Feature Flags
builder.Services.AddImsFeatureFlags(builder.Configuration);

// US-078: Multi-Tenancy
builder.Services.AddMultiTenancy();

// US-080: Tenant catalog (Tenants table + CRUD API)
builder.Services.AddTenantManagement(builder.Configuration);

// US-083: Audit Log
builder.Services.AddAuditModule(builder.Configuration, builder.Environment);

// US-071: Full-text Search (Meilisearch)
builder.Services.AddSearchModule(builder.Configuration);

// US-091: Billing & Subscription
builder.Services.AddBillingModule(builder.Configuration, builder.Environment);

// US-086/092: Background Jobs (Hangfire)
builder.Services.AddJobsModule(builder.Configuration, builder.Environment);

// Epic 2: Smart Assigner — auto-assignment engine
builder.Services.AddSmartAssignerModule(builder.Configuration);

// Epic 1: Warehouse Routing — optimal pickup path calculation
builder.Services.AddWarehouseRoutingModule(builder.Configuration);

// US-079: YARP proxy for Issues microservice (only active when UseIssuesMicroservice flag is on)
builder.Services.AddIssuesProxy(builder.Configuration);

// ============================================================
// HEALTH CHECKS (US-007)
// ============================================================

builder.Services.AddImsHealthChecks(builder.Configuration);

// ============================================================
// RATE LIMITING (US-009)
// ============================================================

builder.Services.AddImsRateLimiting(builder.Configuration);

// ============================================================
// SIGNALR (US-039: Real-Time Notifications)
// ============================================================

builder.Services.AddSignalR();

// ============================================================
// OPENTELEMETRY (US-010: Traces + Metrics + Prometheus)
// ============================================================

builder.Services.AddImsOpenTelemetry(builder.Configuration);

// ============================================================
// BUILD APP
// ============================================================

var app = builder.Build();

// ============================================================
// MIDDLEWARE PIPELINE
// ============================================================

// Cross-cutting middleware (CorrelationId → Metrics → PerformanceTiming)
app.UseImsMiddleware();

// Serilog request logging (after CorrelationId middleware so it captures the ID)
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "IMS Modular API v1");
        c.RoutePrefix = string.Empty;
    });
}

app.UseCors();

// Rate Limiting (US-009 — before auth so rate-limited requests are rejected early)
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

// UserContext middleware (must be after auth — extracts JWT claims into IUserContext)
app.UseUserContext();

// US-078: Multi-Tenancy — resolve tenant após auth (pode usar claim JWT)
app.UseMultiTenancy();

// Output Caching (US-008 — must be after auth so cached responses respect authorization)
app.UseOutputCache();

// ============================================================
// SYSTEM ENDPOINTS
// ============================================================

// Health check endpoints (/health, /health/live, /health/ready)
app.MapImsHealthChecks();

// Prometheus metrics endpoint (/metrics — US-010)
app.MapImsMetrics();

app.MapGet("/api/ping", () => Results.Ok(new
{
    Message = "pong",
    Service = "IMS.Modular",
    Timestamp = DateTime.UtcNow
}))
.WithName("Ping")
.WithTags("System")
.AllowAnonymous();

// US-080: Debug endpoint — returns current TenantContext state for integration tests
app.MapGet("/api/debug/tenant", (IMS.Modular.Shared.MultiTenancy.ITenantService ts) => Results.Ok(new
{
    TenantId = ts.TenantId,
    IsMultiTenancyEnabled = ts.IsMultiTenancyEnabled
}))
.WithName("DebugTenant")
.WithTags("System")
.AllowAnonymous();

app.MapGet("/api/status", () => Results.Ok(new
{
    Status = "Running",
    Version = "1.0.0",
    Environment = app.Environment.EnvironmentName,        Modules = new[] { "Auth", "Issues", "Inventory", "InventoryIssues", "Analytics" },
    Timestamp = DateTime.UtcNow
}))
.WithName("GetStatus")
.WithTags("System")
.AllowAnonymous();

// ============================================================
// MODULE ENDPOINTS
// ============================================================

AuthModule.Map(app);
// US-064: New UserManagement module at /api/users (replaces /api/admin/users)
UserManagementModule.Map(app);
// US-089: LGPD/GDPR endpoints
GdprModule.Map(app);
// Kept for backward compatibility — deprecated, will be removed in Sprint 14
UserAdminModule.Map(app);
IssuesModule.Map(app);
InventoryModule.Map(app);
InventoryIssuesModule.Map(app);
AnalyticsModule.Map(app);
NotificationsModule.Map(app);
app.MapWebhooksModule();
SearchModule.Map(app);
FeaturesModule.Map(app);
// US-091: Billing & Subscription
BillingModule.Map(app);
// Epic 2: Smart Assigner API
SmartAssignerModule.Map(app);
// Epic 1: Warehouse Routing API
WarehouseRoutingModule.Map(app);
// US-080: Tenant management API
TenantEndpoints.Map(app);

// US-086/092: Hangfire dashboard + recurring jobs
app.UseJobsModule();

// US-083: Audit log API
app.MapAuditModule();

// SignalR hub
app.MapHub<NotificationsHub>("/hubs/notifications").AllowAnonymous();

// US-079: YARP proxy — routes /api/issues to issues microservice when feature flag is enabled
var featureManager = app.Services.GetRequiredService<IFeatureManager>();
if (await featureManager.IsEnabledAsync("UseIssuesMicroservice"))
{
    app.MapIssuesProxy();
}

// ============================================================
// DATABASE INITIALIZATION
// ============================================================

try
{
    await app.Services.InitializeAuditModuleAsync();
    await app.Services.InitializeOutboxAsync();
    await app.Services.InitializeTenantDbAsync();           // US-080
    await app.Services.InitializeAuthModuleAsync();
    await app.Services.InitializeIssuesModuleAsync();
    await app.Services.InitializeInventoryModuleAsync();
    await app.Services.InitializeInventoryIssuesModuleAsync();
    await app.Services.InitializeNotificationsModuleAsync();
    await app.Services.InitializeWebhooksModuleAsync();
    await app.Services.InitializeSearchModuleAsync();
    await app.Services.InitializeBillingModuleAsync();   // US-091
}
catch (Exception ex)
{
    Log.Warning(ex, "Database initialization encountered an error (may be expected in test environments)");
}

// ============================================================
// RUN
// ============================================================

app.Run();

}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program { }
