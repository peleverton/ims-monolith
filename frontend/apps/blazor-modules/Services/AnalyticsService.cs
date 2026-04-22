using System.Net.Http.Headers;
using System.Net.Http.Json;
using BlazorModules.Models;

namespace BlazorModules.Services;

public interface IAnalyticsService
{
    Task<AnalyticsSummaryDto?> GetSummaryAsync();
}

public class AnalyticsService(HttpClient http, IAuthBridgeService auth) : IAnalyticsService
{
    public async Task<AnalyticsSummaryDto?> GetSummaryAsync()
    {
        var token = await auth.GetTokenAsync();
        if (!string.IsNullOrEmpty(token))
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Busca em paralelo: dashboard + breakdown por status e prioridade
        var dashboardTask   = http.GetFromJsonAsync<BackendDashboardDto>("/api/proxy/analytics/dashboard");
        var byStatusTask    = http.GetFromJsonAsync<List<BackendIssueStatsByStatusDto>>("/api/proxy/analytics/issues/by-status");
        var byPriorityTask  = http.GetFromJsonAsync<List<BackendIssueStatsByPriorityDto>>("/api/proxy/analytics/issues/by-priority");

        await Task.WhenAll(dashboardTask, byStatusTask, byPriorityTask);

        var dashboard  = dashboardTask.Result;
        var byStatus   = byStatusTask.Result;
        var byPriority = byPriorityTask.Result;

        if (dashboard is null) return null;

        var issuesByStatus   = byStatus?
            .ToDictionary(x => x.Status, x => x.Count)
            ?? new Dictionary<string, int>();

        var issuesByPriority = byPriority?
            .ToDictionary(x => x.Priority, x => x.Count)
            ?? new Dictionary<string, int>();

        // Mapeia RecentTrends → IssuesByDay (usa campo Created como contagem diária)
        var issuesByDay = dashboard.RecentTrends
            .Select(t => new IssuesByDayDto(t.Date, t.Created))
            .ToList();

        var s = dashboard.IssueSummary;
        var inv = dashboard.InventorySummary;

        return new AnalyticsSummaryDto(
            TotalIssues:      s.Total,
            OpenIssues:       s.Open,
            InProgressIssues: s.InProgress + s.Testing,
            ResolvedIssues:   s.Resolved,
            ClosedIssues:     s.Closed,
            TotalInventoryItems: inv.TotalProducts,
            LowStockItems:    inv.LowStockProducts,
            IssuesByStatus:   issuesByStatus,
            IssuesByPriority: issuesByPriority,
            IssuesByDay:      issuesByDay
        );
    }
}
