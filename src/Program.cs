using FluentValidation;
using IMS.Modular.Modules.Auth;
using IMS.Modular.Modules.Auth.Api;
using IMS.Modular.Modules.Issues;
using IMS.Modular.Modules.Issues.Api;
using IMS.Modular.Shared.Abstractions;
using IMS.Modular.Shared.Behaviors;
using IMS.Modular.Shared.Middleware;
using MediatR;
using Microsoft.OpenApi.Models;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

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

    // Pipeline Behaviors (order matters: Validation → Logging → Caching → Handler)
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(CachingBehavior<,>));
});

// FluentValidation (scans all validators in current assembly)
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

// Cache services (in-memory fallback; Redis will replace this in Phase 2)
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ICacheService, InMemoryCacheService>();

// Middleware services (IUserContext, ICorrelationIdAccessor, IHttpContextAccessor)
builder.Services.AddMiddlewareServices();

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

// ============================================================
// MODULE REGISTRATION
// ============================================================

builder.Services.AddAuthModule(builder.Configuration);
builder.Services.AddIssuesModule(builder.Configuration);

// ============================================================
// BUILD APP
// ============================================================

var app = builder.Build();

// ============================================================
// MIDDLEWARE PIPELINE
// ============================================================

// Cross-cutting middleware (CorrelationId → Metrics → PerformanceTiming)
app.UseImsMiddleware();

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
app.UseAuthentication();
app.UseAuthorization();

// UserContext middleware (must be after auth — extracts JWT claims into IUserContext)
app.UseUserContext();

// ============================================================
// SYSTEM ENDPOINTS
// ============================================================

app.MapGet("/api/ping", () => Results.Ok(new
{
    Message = "pong",
    Service = "IMS.Modular",
    Timestamp = DateTime.UtcNow
}))
.WithName("Ping")
.WithTags("System")
.AllowAnonymous();

app.MapGet("/api/status", () => Results.Ok(new
{
    Status = "Running",
    Version = "1.0.0",
    Environment = app.Environment.EnvironmentName,
    Modules = new[] { "Auth", "Issues" },
    Timestamp = DateTime.UtcNow
}))
.WithName("GetStatus")
.WithTags("System")
.AllowAnonymous();

// ============================================================
// MODULE ENDPOINTS
// ============================================================

AuthModule.Map(app);
IssuesModule.Map(app);

// ============================================================
// DATABASE INITIALIZATION
// ============================================================

await app.Services.InitializeAuthModuleAsync();
await app.Services.InitializeIssuesModuleAsync();

// ============================================================
// RUN
// ============================================================

app.Run();

public partial class Program { }
