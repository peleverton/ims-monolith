using Microsoft.AspNetCore.Builder;

namespace IMS.Modular.Tests.Contracts;

/// <summary>
/// US-073: Middleware to handle Pact provider state setup during provider verification.
/// Maps POST /provider-states to seed data required by each consumer interaction.
/// </summary>
public static class ProviderStatesMiddleware
{
    public static void MapProviderStates(this WebApplication app)
    {
        app.MapPost("/provider-states", async (HttpContext context) =>
        {
            using var reader = new StreamReader(context.Request.Body);
            var body = await reader.ReadToEndAsync();
            // In a real setup, parse body and seed the DB accordingly.
            // For now, all states are satisfied by the seeded test data.
            return Results.Ok();
        }).AllowAnonymous();
    }
}
