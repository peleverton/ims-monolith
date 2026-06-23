using IMS.Modular.Modules.AnomalyDetection.Application.DTOs;
using IMS.Modular.Modules.AnomalyDetection.Domain;
using IMS.Modular.Modules.AnomalyDetection.Domain.Entities;
using IMS.Modular.Shared.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace IMS.Modular.Modules.AnomalyDetection.Api;

/// <summary>
/// Epic 4: Shrinkage & Fraud Detection — Detecção de anomalias em movimentações.
/// </summary>
public static class AnomalyDetectionModule
{
    public static IEndpointRouteBuilder Map(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/anomaly-detection")
            .WithTags("Anomaly Detection")
            .RequireAuthorization(Policies.CanManageInventory);

        group.MapGet("/alerts", async (
            IAnomalyAlertRepository repo,
            [FromQuery] string? status = null,
            [FromQuery] int top = 100,
            CancellationToken ct = default) =>
        {
            var alerts = status?.ToLowerInvariant() == "open"
                ? await repo.GetOpenAlertsAsync(ct)
                : await repo.GetAllAsync(top, ct);
            return Results.Ok(alerts.Select(ToDto).ToList());
        }).WithName("GetAnomalyAlerts");

        group.MapGet("/alerts/{id:guid}", async (
            Guid id,
            IAnomalyAlertRepository repo,
            CancellationToken ct) =>
        {
            var alert = await repo.GetByIdAsync(id, ct);
            return alert is null ? Results.NotFound() : Results.Ok(ToDto(alert));
        }).WithName("GetAnomalyAlertById");

        group.MapPatch("/alerts/{id:guid}/acknowledge", async (
            Guid id,
            [FromBody] AcknowledgeAlertRequest request,
            HttpContext http,
            IAnomalyAlertRepository repo,
            CancellationToken ct) =>
        {
            var alert = await repo.GetByIdAsync(id, ct);
            if (alert is null) return Results.NotFound();

            var user = http.User.Identity?.Name ?? "system";
            alert.Acknowledge(user, request.Resolution);
            await repo.SaveChangesAsync(ct);
            return Results.Ok(ToDto(alert));
        }).WithName("AcknowledgeAnomalyAlert");

        group.MapPatch("/alerts/{id:guid}/dismiss", async (
            Guid id,
            [FromBody] DismissAlertRequest request,
            HttpContext http,
            IAnomalyAlertRepository repo,
            CancellationToken ct) =>
        {
            var alert = await repo.GetByIdAsync(id, ct);
            if (alert is null) return Results.NotFound();

            var user = http.User.Identity?.Name ?? "system";
            alert.Dismiss(user, request.Reason);
            await repo.SaveChangesAsync(ct);
            return Results.Ok(ToDto(alert));
        }).WithName("DismissAnomalyAlert");

        group.MapPatch("/alerts/{id:guid}/escalate", async (
            Guid id,
            IAnomalyAlertRepository repo,
            CancellationToken ct) =>
        {
            var alert = await repo.GetByIdAsync(id, ct);
            if (alert is null) return Results.NotFound();

            alert.Escalate();
            await repo.SaveChangesAsync(ct);
            return Results.Ok(ToDto(alert));
        }).WithName("EscalateAnomalyAlert");

        group.MapGet("/summary", async (IAnomalyAlertRepository repo, CancellationToken ct) =>
        {
            var all = await repo.GetAllAsync(int.MaxValue, ct);
            var summary = new AnomalyStatsSummaryDto(
                TotalOpen: all.Count(a => a.Status == AlertStatus.Open),
                TotalAcknowledged: all.Count(a => a.Status == AlertStatus.Acknowledged),
                TotalEscalated: all.Count(a => a.Status == AlertStatus.Escalated),
                TotalDismissed: all.Count(a => a.Status == AlertStatus.Dismissed),
                CriticalOpen: all.Count(a => a.Status == AlertStatus.Open && a.Severity == AlertSeverity.Critical),
                HighOpen: all.Count(a => a.Status == AlertStatus.Open && a.Severity == AlertSeverity.High));
            return Results.Ok(summary);
        }).WithName("GetAnomalySummary");

        return endpoints;
    }

    private static AnomalyAlertDto ToDto(AnomalyAlert a) => new(
        a.Id, a.UserId, a.UserName, a.LocationId, a.LocationName,
        a.Type, a.Severity, a.Status,
        a.Description, a.OccurrenceCount, a.WindowDescription,
        a.CreatedAt, a.AcknowledgedAt, a.AcknowledgedBy, a.Resolution);
}
