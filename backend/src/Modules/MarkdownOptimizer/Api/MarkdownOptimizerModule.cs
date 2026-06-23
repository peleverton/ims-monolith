using IMS.Modular.Modules.MarkdownOptimizer.Application.DTOs;
using IMS.Modular.Modules.MarkdownOptimizer.Domain;
using IMS.Modular.Modules.MarkdownOptimizer.Domain.Entities;
using IMS.Modular.Shared.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace IMS.Modular.Modules.MarkdownOptimizer.Api;

/// <summary>
/// Epic 3: Markdown Optimizer — Precificação dinâmica por validade.
/// Endpoints para gerenciamento de regras e consulta de aplicações.
/// </summary>
public static class MarkdownOptimizerModule
{
    public static IEndpointRouteBuilder Map(IEndpointRouteBuilder endpoints)
    {
        MapRules(endpoints);
        MapApplications(endpoints);
        return endpoints;
    }

    private static void MapRules(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/markdown/rules")
            .WithTags("Markdown Optimizer - Rules")
            .RequireAuthorization(Policies.CanManageInventory);

        group.MapGet("/", async (IMarkdownRuleRepository repo, CancellationToken ct) =>
        {
            var rules = await repo.GetAllAsync(ct);
            return Results.Ok(rules.Select(ToRuleDto).ToList());
        }).WithName("GetMarkdownRules");

        group.MapGet("/{id:guid}", async (Guid id, IMarkdownRuleRepository repo, CancellationToken ct) =>
        {
            var rule = await repo.GetByIdAsync(id, ct);
            return rule is null ? Results.NotFound() : Results.Ok(ToRuleDto(rule));
        }).WithName("GetMarkdownRuleById");

        group.MapPost("/", async (
            [FromBody] CreateMarkdownRuleRequest request,
            IMarkdownRuleRepository repo,
            CancellationToken ct) =>
        {
            var rule = new MarkdownRule(
                request.Name,
                request.Priority,
                request.DaysToExpiryThreshold,
                request.MinimumStockThreshold,
                request.DiscountPercent);

            await repo.AddAsync(rule, ct);
            await repo.SaveChangesAsync(ct);
            return Results.Created($"/api/markdown/rules/{rule.Id}", ToRuleDto(rule));
        }).WithName("CreateMarkdownRule");

        group.MapPut("/{id:guid}", async (
            Guid id,
            [FromBody] UpdateMarkdownRuleRequest request,
            IMarkdownRuleRepository repo,
            CancellationToken ct) =>
        {
            var rule = await repo.GetByIdAsync(id, ct);
            if (rule is null) return Results.NotFound();

            rule.Update(
                request.Name,
                request.Priority,
                request.DaysToExpiryThreshold,
                request.MinimumStockThreshold,
                request.DiscountPercent);

            await repo.SaveChangesAsync(ct);
            return Results.Ok(ToRuleDto(rule));
        }).WithName("UpdateMarkdownRule");

        group.MapPatch("/{id:guid}/activate", async (Guid id, IMarkdownRuleRepository repo, CancellationToken ct) =>
        {
            var rule = await repo.GetByIdAsync(id, ct);
            if (rule is null) return Results.NotFound();
            rule.Activate();
            await repo.SaveChangesAsync(ct);
            return Results.NoContent();
        }).WithName("ActivateMarkdownRule");

        group.MapPatch("/{id:guid}/deactivate", async (Guid id, IMarkdownRuleRepository repo, CancellationToken ct) =>
        {
            var rule = await repo.GetByIdAsync(id, ct);
            if (rule is null) return Results.NotFound();
            rule.Deactivate();
            await repo.SaveChangesAsync(ct);
            return Results.NoContent();
        }).WithName("DeactivateMarkdownRule");
    }

    private static void MapApplications(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/markdown/applications")
            .WithTags("Markdown Optimizer - Applications")
            .RequireAuthorization(Policies.CanViewInventory);

        group.MapGet("/", async (
            IMarkdownApplicationRepository repo,
            [FromQuery] int count = 50,
            CancellationToken ct = default) =>
        {
            var apps = await repo.GetRecentAsync(count, ct);
            return Results.Ok(apps.Select(ToAppDto).ToList());
        }).WithName("GetRecentMarkdownApplications");

        group.MapGet("/product/{productId:guid}", async (
            Guid productId,
            IMarkdownApplicationRepository repo,
            CancellationToken ct) =>
        {
            var apps = await repo.GetByProductAsync(productId, ct);
            return Results.Ok(apps.Select(ToAppDto).ToList());
        }).WithName("GetMarkdownApplicationsByProduct");
    }

    private static MarkdownRuleDto ToRuleDto(MarkdownRule r) => new(
        r.Id, r.Name, r.Priority, r.IsActive,
        r.DaysToExpiryThreshold, r.MinimumStockThreshold,
        r.DiscountPercent, r.CreatedAt);

    private static MarkdownApplicationDto ToAppDto(MarkdownApplication a) => new(
        a.Id, a.ProductId, a.SKU, a.RuleName,
        a.OriginalPrice, a.DiscountedPrice, a.DiscountPercent,
        a.ExpiryDate, a.DaysToExpiryAtApplication,
        a.StockAtApplication, a.AppliedAt);
}
