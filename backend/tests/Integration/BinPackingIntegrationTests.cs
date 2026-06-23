using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace IMS.Modular.Tests.Integration;

[Collection("Integration")]
public class BinPackingIntegrationTests : IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly IntegrationWebAppFactory _factory;

    public BinPackingIntegrationTests(IntegrationWebAppFactory factory)
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
    public async Task GetPackagingTypes_Unauthenticated_Returns401()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/bin-packing/packaging-types");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetPackagingTypes_Returns200()
    {
        var response = await _client.GetAsync("/api/bin-packing/packaging-types");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreatePackagingType_Returns201()
    {
        var request = new
        {
            Name = "Caixa Pequena",
            Code = $"BOX-S-{Guid.NewGuid():N}"[..20],
            MaxLengthCm = 30m,
            MaxWidthCm = 20m,
            MaxHeightCm = 15m,
            MaxWeightKg = 5m,
            CostPerUnit = 2.50m
        };

        var response = await _client.PostAsJsonAsync("/api/bin-packing/packaging-types", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("name").GetString().Should().Be(request.Name);
        body.GetProperty("isActive").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task CreateAndGetById_PackagingType_Roundtrip()
    {
        var request = new
        {
            Name = "Caixa Média",
            Code = $"BOX-M-{Guid.NewGuid():N}"[..20],
            MaxLengthCm = 50m,
            MaxWidthCm = 40m,
            MaxHeightCm = 30m,
            MaxWeightKg = 15m,
            CostPerUnit = 5.00m
        };

        var createResp = await _client.PostAsJsonAsync("/api/bin-packing/packaging-types", request);
        var created = await createResp.Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetString();

        var getResp = await _client.GetAsync($"/api/bin-packing/packaging-types/{id}");
        getResp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeactivatePackagingType_Returns204()
    {
        var request = new
        {
            Name = "Caixa Descartável",
            Code = $"BOX-D-{Guid.NewGuid():N}"[..20],
            MaxLengthCm = 20m,
            MaxWidthCm = 15m,
            MaxHeightCm = 10m,
            MaxWeightKg = 3m,
            CostPerUnit = 1.00m
        };

        var createResp = await _client.PostAsJsonAsync("/api/bin-packing/packaging-types", request);
        var created = await createResp.Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetString();

        var deactivateResp = await _client.PatchAsync($"/api/bin-packing/packaging-types/{id}/deactivate", null);
        deactivateResp.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Suggest_EmptyItems_ReturnsBadRequest()
    {
        var request = new { Items = new object[] { } };
        var response = await _client.PostAsJsonAsync("/api/bin-packing/suggest", request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Suggest_NonExistentProduct_Returns200Or422()
    {
        var request = new
        {
            Items = new[] { new { ProductId = Guid.NewGuid(), Quantity = 1 } }
        };
        var response = await _client.PostAsJsonAsync("/api/bin-packing/suggest", request);
        // No products found → either 422 (no boxes) or 200 (empty suggestion)
        ((int)response.StatusCode).Should().BeOneOf(200, 422);
    }
}
