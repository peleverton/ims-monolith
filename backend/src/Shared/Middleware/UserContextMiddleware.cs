using System.Security.Claims;
using IMS.Modular.Shared.Abstractions;

namespace IMS.Modular.Shared.Middleware;

public sealed class UserContext : IUserContext
{
    public Guid? UserId { get; set; }
    public string? Email { get; set; }
    public IReadOnlyList<string> Roles { get; set; } = [];
    public bool IsAuthenticated => UserId.HasValue;
    public bool IsAdmin => Roles.Contains("Admin");
}

/// <summary>
/// Extracts JWT claims (UserId, Email, Roles) from the authenticated user
/// and populates the scoped IUserContext for downstream use.
/// Must run after UseAuthentication() + UseAuthorization().
/// </summary>
public sealed class UserContextMiddleware
{
    private readonly RequestDelegate _next;

    public UserContextMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, UserContext userContext)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var claims = context.User.Claims.ToList();

            // Extract UserId from sub or nameidentifier claim
            var userIdClaim = claims.FirstOrDefault(c =>
                c.Type == ClaimTypes.NameIdentifier || c.Type == "sub")?.Value;

            if (Guid.TryParse(userIdClaim, out var userId))
                userContext.UserId = userId;

            // Extract Email
            userContext.Email = claims.FirstOrDefault(c =>
                c.Type == ClaimTypes.Email || c.Type == "email")?.Value;

            // Extract Roles
            userContext.Roles = claims
                .Where(c => c.Type == ClaimTypes.Role || c.Type == "role")
                .Select(c => c.Value)
                .ToList()
                .AsReadOnly();
        }

        await _next(context);
    }
}
