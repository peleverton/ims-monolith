using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace IMS.Modular.Tests.Integration;

[Collection("Integration")]
public class MarkdownOptimizerIntegrationTests : IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly IntegrationWebAppFactory _factory;

    public MarkdownOptimizerIntegrationTests(IntegrationWebAppFactory factory)
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
    public async Task GetRules_Unauthenticated_Returns401()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/markdown/rules");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetRules_Returns200()
    {
        var response = await _client.GetAsync("/api/markdown/rules");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        content.ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public async Task CreateRule_Returns201()
    {
        var request = new
        {
            Name = "Regra Teste 7 dias",
            Priority = 10,
            DaysToExpiryThreshold = 7,
            MinimumStockThreshold = 50,
            DiscountPercent = 15.0m
        };

        var response = await _client.PostAsJsonAsync("/api/markdown/rules", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("name").GetString().Should().Be(request.Name);
        body.GetProperty("discountPercent").GetDecimal().Should().Be(15.0m);
    }

    [Fact]
    public async Task CreateAndUpdateRule_Roundtrip()
    {
        var create = new
        {
            Name = "Regra Update Test",
            Priority = 5,
            DaysToExpiryThreshold = 14,
            MinimumStockThreshold = 100,
            DiscountPercent = 20.0m
        };

        var createResp = await _client.PostAsJsonAsync("/api/markdown/rules", create);
        var created = await createResp.Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetString();

        var update = new
        {
            Name = "Regra Atualizada",
            Priority = 1,
            DaysToExpiryThreshold = 3,
            MinimumStockThreshold = 200,
            DiscountPercent = 30.0m
        };

        var updateResp = await _client.PutAsJsonAsync($"/api/markdown/rules/{id}", update);
        updateResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await updateResp.Content.ReadFromJsonAsync<JsonElement>();
        updated.GetProperty("name").GetString().Should().Be("Regra Atualizada");
        updated.GetProperty("discountPercent").GetDecimal().Should().Be(30.0m);
    }

    [Fact]
    public async Task DeactivateAndActivateRule_WorksCorrectly()
    {
        var create = new
        {
            Name = "Regra Toggle",
            Priority = 99,
            DaysToExpiryThreshold = 30,
            MinimumStockThreshold = 10,
            DiscountPercent = 5.0m
        };

        var createResp = await _client.PostAsJsonAsync("/api/markdown/rules", create);
        var created = await createResp.Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetString();

        // Deactivate
        var deactivateResp = await _client.PatchAsync($"/api/markdown/rules/{id}/deactivate", null);
        deactivateResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify deactivated
        var getResp = await _client.GetAsync($"/api/markdown/rules/{id}");
        var rule = await getResp.Content.ReadFromJsonAsync<JsonElement>();
        rule.GetProperty("isActive").GetBoolean().Should().BeFalse();

        // Activate
        var activateResp = await _client.PatchAsync($"/api/markdown/rules/{id}/activate", null);
        activateResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify active
        var getResp2 = await _client.GetAsync($"/api/markdown/rules/{id}");
        var rule2 = await getResp2.Content.ReadFromJsonAsync<JsonElement>();
        rule2.GetProperty("isActive").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task GetApplications_Returns200()
    {
        var response = await _client.GetAsync("/api/markdown/applications");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        content.ValueKind.Should().Be(JsonValueKind.Array);
    }
}
