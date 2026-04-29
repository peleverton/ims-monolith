using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace IMS.Modular.Tests.Integration;

/// <summary>
/// Testes de integração que verificam dados pré-populados pelo seed.
/// Cobrem GET por ID, listagens filtradas e comportamentos de domínio
/// (ex: produto com estoque abaixo do mínimo aparece como LowStock).
/// </summary>
[Collection("Integration")]
public class InventorySeededDataTests : IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly IntegrationWebAppFactory _factory;

    public InventorySeededDataTests(IntegrationWebAppFactory factory)
    {
        _factory = factory;
        _client  = factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        await _factory.EnsureAdminTokenAsync();
        if (!string.IsNullOrEmpty(_factory.AdminToken))
            _client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _factory.AdminToken);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // ── Produtos pré-seedados ─────────────────────────────────────────────

    [Fact]
    public async Task GetProducts_Returns_SeededProducts()
    {
        var response = await _client.GetAsync("/api/inventory/products");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("totalCount").GetInt32().Should().BeGreaterThanOrEqualTo(3);

        var items = body.GetProperty("items").EnumerateArray().ToList();
        items.Should().Contain(p => p.GetProperty("sku").GetString() == "NB-PRO-450");
        items.Should().Contain(p => p.GetProperty("sku").GetString() == "MOUSE-USB-01");
        items.Should().Contain(p => p.GetProperty("sku").GetString() == "CAFE-500G");
    }

    [Fact]
    public async Task GetProductById_Laptop_ReturnsCorrectData()
    {
        var id = IntegrationSeedData.ProductLaptopId;
        var response = await _client.GetAsync($"/api/inventory/products/{id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var product = await response.Content.ReadFromJsonAsync<JsonElement>();
        product.GetProperty("name").GetString().Should().Be("Notebook ProBook 450");
        product.GetProperty("sku").GetString().Should().Be("NB-PRO-450");
        product.GetProperty("currentStock").GetInt32().Should().Be(20);
        product.GetProperty("unitPrice").GetDecimal().Should().Be(3499.99m);
        product.GetProperty("isActive").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task GetProductBySku_Mouse_ReturnsCorrectData()
    {
        var response = await _client.GetAsync("/api/inventory/products/sku/MOUSE-USB-01");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var product = await response.Content.ReadFromJsonAsync<JsonElement>();
        product.GetProperty("name").GetString().Should().Be("Mouse Óptico USB");
        product.GetProperty("currentStock").GetInt32().Should().Be(150);
    }

    [Fact]
    public async Task GetProducts_FilterByCategory_Electronics_ReturnsTwoProducts()
    {
        var response = await _client.GetAsync("/api/inventory/products?category=Electronics");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("totalCount").GetInt32().Should().BeGreaterThanOrEqualTo(2);

        var items = body.GetProperty("items").EnumerateArray().ToList();
        items.Should().OnlyContain(p => p.GetProperty("category").GetString() == "Electronics");
    }

    [Fact]
    public async Task GetProducts_CoffeeLowStock_StockStatusIsLowStock()
    {
        // Café foi seedado com estoque 3, mínimo 20 → deve aparecer como LowStock
        var id = IntegrationSeedData.ProductCoffeeId;
        var response = await _client.GetAsync($"/api/inventory/products/{id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var product = await response.Content.ReadFromJsonAsync<JsonElement>();
        product.GetProperty("stockStatus").GetString().Should().Be("LowStock");
        product.GetProperty("currentStock").GetInt32().Should().Be(3);
    }

    // ── Fornecedores pré-seedados ─────────────────────────────────────────

    [Fact]
    public async Task GetSuppliers_Returns_SeededSuppliers()
    {
        var response = await _client.GetAsync("/api/inventory/suppliers");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("totalCount").GetInt32().Should().BeGreaterThanOrEqualTo(2);

        var items = body.GetProperty("items").EnumerateArray().ToList();
        items.Should().Contain(s => s.GetProperty("code").GetString() == "TECH-001");
        items.Should().Contain(s => s.GetProperty("code").GetString() == "FOOD-001");
    }

    // ── Locais pré-seedados ───────────────────────────────────────────────

    [Fact]
    public async Task GetLocations_Returns_SeededLocations()
    {
        var response = await _client.GetAsync("/api/inventory/locations");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("totalCount").GetInt32().Should().BeGreaterThanOrEqualTo(2);

        var items = body.GetProperty("items").EnumerateArray().ToList();
        items.Should().Contain(l => l.GetProperty("code").GetString() == "WH-A");
        items.Should().Contain(l => l.GetProperty("code").GetString() == "SHELF-A1");
    }

    // ── Movimentações de estoque ──────────────────────────────────────────

    [Fact]
    public async Task GetStockMovements_ForLaptop_ReturnsInitialStockMovement()
    {
        var id = IntegrationSeedData.ProductLaptopId;
        var response = await _client.GetAsync($"/api/inventory/stock-movements?productId={id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("totalCount").GetInt32().Should().BeGreaterThanOrEqualTo(1);

        var movements = body.GetProperty("items").EnumerateArray().ToList();
        movements.Should().Contain(m =>
            m.GetProperty("movementType").GetString() == "InitialStock" &&
            m.GetProperty("quantity").GetInt32() == 20);
    }
}
