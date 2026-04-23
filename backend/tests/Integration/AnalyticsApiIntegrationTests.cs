using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace IMS.Modular.Tests.Integration;

/// <summary>
/// US-054: Integration tests for Analytics API endpoints.
/// Validates that analytics endpoints return 200 with valid data after seed.
/// Uses IntegrationWebAppFactory with isolated SQLite DBs.
/// </summary>
public class AnalyticsApiIntegrationTests : IClassFixture<IntegrationWebAppFactory>, IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly IntegrationWebAppFactory _factory;

    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public AnalyticsApiIntegrationTests(IntegrationWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        await _factory.EnsureAdminTokenAsync();
        if (!string.IsNullOrEmpty(_factory.AdminToken))
        {
            _client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _factory.AdminToken);
        }
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // ── Authentication guard ──────────────────────────────────────

    [Fact]
    public async Task AnalyticsEndpoints_Unauthenticated_Returns401()
    {
        using var unauthClient = _factory.CreateClient();
        var response = await unauthClient.GetAsync("/api/analytics/issues/summary");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── GET /api/analytics/issues/summary ────────────────────────

    [Fact]
    public async Task GetIssueSummary_Authenticated_Returns200WithValidStructure()
    {
        var response = await _client.GetAsync("/api/analytics/issues/summary");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        body.TryGetProperty("total", out _).Should().BeTrue();
        body.TryGetProperty("open", out _).Should().BeTrue();
        body.TryGetProperty("resolved", out _).Should().BeTrue();
    }

    // ── GET /api/analytics/issues/trends ─────────────────────────

    [Fact]
    public async Task GetIssueTrends_Returns200WithArray()
    {
        var response = await _client.GetAsync("/api/analytics/issues/trends?days=7");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        body.ValueKind.Should().Be(JsonValueKind.Array);
    }

    // ── GET /api/analytics/issues/by-status ──────────────────────

    [Fact]
    public async Task GetIssueStatsByStatus_Returns200WithArray()
    {
        var response = await _client.GetAsync("/api/analytics/issues/by-status");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        body.ValueKind.Should().Be(JsonValueKind.Array);
    }

    // ── GET /api/analytics/issues/by-priority ────────────────────

    [Fact]
    public async Task GetIssueStatsByPriority_Returns200WithArray()
    {
        var response = await _client.GetAsync("/api/analytics/issues/by-priority");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        body.ValueKind.Should().Be(JsonValueKind.Array);
    }

    // ── GET /api/analytics/dashboard ─────────────────────────────

    [Fact]
    public async Task GetDashboard_Returns200WithCompositeStructure()
    {
        var response = await _client.GetAsync("/api/analytics/dashboard");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        body.TryGetProperty("issueSummary", out _).Should().BeTrue();
        body.TryGetProperty("inventorySummary", out _).Should().BeTrue();
        body.TryGetProperty("recentTrends", out _).Should().BeTrue();
        body.TryGetProperty("topAssignees", out _).Should().BeTrue();
        body.TryGetProperty("generatedAt", out _).Should().BeTrue();
    }

    // ── GET /api/analytics/workload ───────────────────────────────

    [Fact]
    public async Task GetWorkload_Returns200WithArray()
    {
        var response = await _client.GetAsync("/api/analytics/workload");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        body.ValueKind.Should().Be(JsonValueKind.Array);
    }

    // ── GET /api/analytics/inventory/summary ─────────────────────

    [Fact]
    public async Task GetInventorySummary_Returns200WithValidStructure()
    {
        var response = await _client.GetAsync("/api/analytics/inventory/summary");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        body.TryGetProperty("totalProducts", out _).Should().BeTrue();
        body.TryGetProperty("activeProducts", out _).Should().BeTrue();
    }

    // ── GET /api/analytics/inventory/stock-status ────────────────

    [Fact]
    public async Task GetStockStatusDistribution_Returns200WithArray()
    {
        var response = await _client.GetAsync("/api/analytics/inventory/stock-status");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        body.ValueKind.Should().Be(JsonValueKind.Array);
    }

    // ── GET /api/analytics/export ─────────────────────────────────

    [Fact]
    public async Task ExportData_Json_Returns200WithBinaryContent()
    {
        var response = await _client.GetAsync("/api/analytics/export?format=json&module=issues");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().BeOneOf("application/json", "application/octet-stream");
    }

    [Fact]
    public async Task ExportData_Csv_Returns200WithCsvContentType()
    {
        var response = await _client.GetAsync("/api/analytics/export?format=csv&module=issues");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().BeOneOf("text/csv", "application/octet-stream");
    }
}
