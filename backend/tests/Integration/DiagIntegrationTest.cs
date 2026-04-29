using System.Net.Http.Json;
using Xunit;
namespace IMS.Modular.Tests.Integration;
[Collection("Integration")]
public class DiagIntegrationTest : IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly IntegrationWebAppFactory _factory;
    public DiagIntegrationTest(IntegrationWebAppFactory factory) { _factory = factory; _client = factory.CreateClient(); }
    public async Task InitializeAsync() { await _factory.EnsureAdminTokenAsync(); if (!string.IsNullOrEmpty(_factory.AdminToken)) _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _factory.AdminToken); }
    public Task DisposeAsync() => Task.CompletedTask;
    [Fact]
    public async Task Diag_GetProductById_ShowBody()
    {
        var id = IntegrationSeedData.ProductLaptopId;
        var response = await _client.GetAsync($"/api/inventory/products/{id}");
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, $"Status: {response.StatusCode}\nBody: {body}");
    }
}
