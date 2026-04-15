using FluentValidation;
using IMS.Modular.Modules.Auth.Application.DTOs;
using IMS.Modular.Modules.Auth.Application.Services;
using IMS.Modular.Shared.Abstractions;
using IMS.Modular.Shared.RateLimiting;
using System.Security.Claims;

namespace IMS.Modular.Modules.Auth.Api;

public class AuthModule : IEndpointModule
{
    public static IEndpointRouteBuilder Map(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/auth")
            .WithTags("Authentication");

        group.MapPost("/login", Login).AllowAnonymous().RequireRateLimiting(RateLimitingExtensions.Policies.Auth).WithName("Login");
        group.MapPost("/register", Register).AllowAnonymous().RequireRateLimiting(RateLimitingExtensions.Policies.Auth).WithName("Register");
        group.MapPost("/refresh", Refresh).AllowAnonymous().WithName("RefreshToken");
        group.MapGet("/me", GetMe).RequireAuthorization().WithName("GetMe");
        group.MapGet("/test", TestAuth).RequireAuthorization().WithName("TestAuth");

        return endpoints;
    }

    private static async Task<IResult> Login(
        LoginRequest request,
        IValidator<LoginRequest> validator,
        IAuthenticationService authService,
        ILogger<AuthModule> logger)
    {
        var validation = await validator.ValidateAsync(request);
        if (!validation.IsValid)
            return Results.ValidationProblem(validation.ToDictionary());

        logger.LogInformation("Login attempt for user: {Username}", request.Username);

        var result = await authService.LoginAsync(request);
        if (result is null)
        {
            logger.LogWarning("Failed login attempt for user: {Username}", request.Username);
            return Results.Unauthorized();
        }

        logger.LogInformation("User {Username} logged in successfully", request.Username);
        return Results.Ok(result);
    }

    private static async Task<IResult> Register(
        RegisterRequest request,
        IValidator<RegisterRequest> validator,
        IAuthenticationService authService,
        ILogger<AuthModule> logger)
    {
        var validation = await validator.ValidateAsync(request);
        if (!validation.IsValid)
            return Results.ValidationProblem(validation.ToDictionary());

        logger.LogInformation("Registration attempt for user: {Username}", request.Username);

        var result = await authService.RegisterAsync(request);
        if (result is null)
        {
            logger.LogWarning("Failed registration for user: {Username}", request.Username);
            return Results.BadRequest(new { message = "Username or email already exists" });
        }

        logger.LogInformation("User {Username} registered successfully", request.Username);
        return Results.Ok(result);
    }

    private static async Task<IResult> Refresh(
        RefreshTokenRequest request,
        IAuthenticationService authService,
        ILogger<AuthModule> logger)
    {
        var result = await authService.RefreshTokenAsync(request.RefreshToken);
        if (result is null)
        {
            logger.LogWarning("Failed token refresh");
            return Results.Unauthorized();
        }

        return Results.Ok(result);
    }

    private static async Task<IResult> GetMe(
        ClaimsPrincipal user,
        IAuthenticationService authService)
    {
        var username = user.Identity?.Name;
        if (string.IsNullOrEmpty(username))
            return Results.Unauthorized();

        var userInfo = await authService.GetUserInfoAsync(username);
        return userInfo is null ? Results.NotFound() : Results.Ok(userInfo);
    }

    private static IResult TestAuth(ClaimsPrincipal user)
    {
        return Results.Ok(new
        {
            message = "Authentication successful",
            username = user.Identity?.Name,
            roles = user.Claims
                .Where(c => c.Type == ClaimTypes.Role)
                .Select(c => c.Value)
                .ToArray(),
            claims = user.Claims.Select(c => new { c.Type, c.Value }).ToArray()
        });
    }
}
