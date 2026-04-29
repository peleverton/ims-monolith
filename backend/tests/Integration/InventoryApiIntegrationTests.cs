using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using IMS.Modular.Modules.Inventory.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using IMS.Modular.Modules.Inventory.Infrastructure;
using IMS.Modular.Modules.Issues.Infrastructure;
using IMS.Modular.Modules.Auth.Infrastructure;

namespace IMS.Modular.Tests.Integration;

/// <summary>
/// Integration tests for Inventory API endpoints.
/// Uses WebApplicationFactory with in-memory DB for isolation.
/// </summary>
[Collection("Integration")]
public class InventoryApiIntegrationTests : IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly IntegrationWebAppFactory _factory;
    private string _authToken = string.Empty;

    public InventoryApiIntegrationTests(IntegrationWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        // Obtém o token uma vez (cacheado na factory) e aplica ao client
        await _factory.EnsureAdminTokenAsync();
        if (!string.IsNullOrEmpty(_factory.AdminToken))
        {
            _client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _factory.AdminToken);
        }
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // ── GET /api/inventory/products ───────────────────────────────

    [Fact]
    public async Task GetProducts_Authenticated_Returns200()
    {
        var response = await _client.GetAsync("/api/inventory/products");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetProducts_Unauthenticated_Returns401()
    {
        using var unauthClient = _factory.CreateClient();
        var response = await unauthClient.GetAsync("/api/inventory/products");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── POST /api/inventory/products ──────────────────────────────

    [Fact]
    public async Task CreateProduct_ValidPayload_Returns201()
    {
        var payload = new
        {
            name = "Integration Test Widget",
            sku = $"INT-{Guid.NewGuid().ToString("N")[..8]}",
            category = "Electronics",
            minimumStockLevel = 5,
            maximumStockLevel = 200,
            unitPrice = 99.99,
            costPrice = 50.00,
            unit = "un",
            currency = "BRL"
        };

        var response = await _client.PostAsJsonAsync("/api/inventory/products", payload);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("name").GetString().Should().Be("Integration Test Widget");
    }

    [Fact]
    public async Task CreateProduct_MissingName_Returns400()
    {
        var payload = new
        {
            sku = "INT-NONAME",
            category = "Electronics",
            minimumStockLevel = 5,
            maximumStockLevel = 100,
            unitPrice = 10.0,
            costPrice = 5.0,
            unit = "un",
            currency = "BRL"
        };

        var response = await _client.PostAsJsonAsync("/api/inventory/products", payload);
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task CreateProduct_DuplicateSKU_Returns409()
    {
        var sku = $"DUP-{Guid.NewGuid().ToString("N")[..8]}";
        var payload = new
        {
            name = "Dup Product",
            sku,
            category = "Electronics",
            minimumStockLevel = 5,
            maximumStockLevel = 100,
            unitPrice = 10.0,
            costPrice = 5.0,
            unit = "un",
            currency = "BRL"
        };

        await _client.PostAsJsonAsync("/api/inventory/products", payload);
        var duplicate = await _client.PostAsJsonAsync("/api/inventory/products", payload);

        duplicate.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // ── PATCH /api/inventory/products/{id}/stock ──────────────────

    [Fact]
    public async Task AdjustStock_StockIn_ReturnsUpdatedStock()
    {
        // Create a product first
        var createPayload = new
        {
            name = "Stock Test Product",
            sku = $"STK-{Guid.NewGuid().ToString("N")[..8]}",
            category = "Electronics",
            minimumStockLevel = 5,
            maximumStockLevel = 500,
            unitPrice = 10.0,
            costPrice = 5.0,
            unit = "un",
            currency = "BRL"
        };

        var createResponse = await _client.PostAsJsonAsync("/api/inventory/products", createPayload);
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var productId = created.GetProperty("id").GetGuid();

        // Adjust stock
        var adjustPayload = new
        {
            quantity = 50,
            movementType = "StockIn"
        };

        var adjustResponse = await _client.PatchAsJsonAsync(
            $"/api/inventory/products/{productId}/stock/adjust", adjustPayload);

        adjustResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var adjusted = await adjustResponse.Content.ReadFromJsonAsync<JsonElement>();
        adjusted.GetProperty("currentStock").GetInt32().Should().Be(50);
    }

    // ── GET /api/inventory/products/{id} ──────────────────────────

    [Fact]
    public async Task GetProductById_NonExistent_Returns404()
    {
        var response = await _client.GetAsync($"/api/inventory/products/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── DELETE /api/inventory/products/{id} ───────────────────────

    [Fact]
    public async Task DeleteProduct_ExistingProduct_Returns204()
    {
        var createPayload = new
        {
            name = "Delete Me",
            sku = $"DEL-{Guid.NewGuid().ToString("N")[..8]}",
            category = "Electronics",
            minimumStockLevel = 1,
            maximumStockLevel = 10,
            unitPrice = 5.0,
            costPrice = 2.0,
            unit = "un",
            currency = "BRL"
        };

        var createResponse = await _client.PostAsJsonAsync("/api/inventory/products", createPayload);
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var productId = created.GetProperty("id").GetGuid();

        var deleteResponse = await _client.DeleteAsync($"/api/inventory/products/{productId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
