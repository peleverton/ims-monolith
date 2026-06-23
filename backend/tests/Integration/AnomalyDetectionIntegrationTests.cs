using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace IMS.Modular.Tests.Integration;

[Collection("Integration")]
public class AnomalyDetectionIntegrationTests : IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly IntegrationWebAppFactory _factory;

    public AnomalyDetectionIntegrationTests(IntegrationWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        await _factory.EnsureAdminTokenAsync();
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _factory.AdminToken);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetAlerts_Unauthenticated_Returns401()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/anomaly-detection/alerts");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAlerts_Returns200()
    {
        var response = await _client.GetAsync("/api/anomaly-detection/alerts");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        content.ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public async Task GetAlerts_FilterByStatus_Returns200()
    {
        var response = await _client.GetAsync("/api/anomaly-detection/alerts?status=open");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAlertById_NotFound_Returns404()
    {
        var response = await _client.GetAsync($"/api/anomaly-detection/alerts/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetSummary_Returns200WithExpectedShape()
    {
        var response = await _client.GetAsync("/api/anomaly-detection/summary");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        content.TryGetProperty("totalOpen", out _).Should().BeTrue();
    }

    [Fact]
    public async Task AcknowledgeAlert_NotFound_Returns404()
    {
        var request = new { Resolution = "Investigado — falso positivo." };
        var response = await _client.PatchAsJsonAsync(
            $"/api/anomaly-detection/alerts/{Guid.NewGuid()}/acknowledge", request);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DismissAlert_NotFound_Returns404()
    {
        var request = new { Reason = "Duplicado." };
        var response = await _client.PatchAsJsonAsync(
            $"/api/anomaly-detection/alerts/{Guid.NewGuid()}/dismiss", request);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
