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

        return await http.GetFromJsonAsync<AnalyticsSummaryDto>("/api/proxy/analytics/summary");
    }
}
