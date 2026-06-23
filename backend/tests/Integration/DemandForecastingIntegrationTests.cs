using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace IMS.Modular.Tests.Integration;

[Collection("Integration")]
public class DemandForecastingIntegrationTests : IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly IntegrationWebAppFactory _factory;

    public DemandForecastingIntegrationTests(IntegrationWebAppFactory factory)
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
    public async Task GetForecasts_Unauthenticated_Returns401()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/demand-forecasting/forecasts");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetForecasts_Returns200()
    {
        var response = await _client.GetAsync("/api/demand-forecasting/forecasts");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        content.ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public async Task GetForecastByProduct_NotFound_Returns404()
    {
        var response = await _client.GetAsync($"/api/demand-forecasting/forecasts/product/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetForecastsByRisk_Returns200()
    {
        var response = await _client.GetAsync("/api/demand-forecasting/forecasts/risk/Low");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        content.ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public async Task GetSummary_Returns200WithExpectedShape()
    {
        var response = await _client.GetAsync("/api/demand-forecasting/summary");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        content.TryGetProperty("totalProducts", out _).Should().BeTrue();
        content.TryGetProperty("critical", out _).Should().BeTrue();
        content.TryGetProperty("high", out _).Should().BeTrue();
    }

    [Fact]
    public async Task PostRecalculate_Returns202()
    {
        var response = await _client.PostAsync("/api/demand-forecasting/recalculate", null);
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }
}
