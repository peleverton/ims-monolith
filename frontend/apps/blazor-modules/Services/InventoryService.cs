using System.Net.Http.Headers;
using System.Net.Http.Json;
using BlazorModules.Models;

namespace BlazorModules.Services;

public interface IInventoryService
{
    Task<PagedResult<InventoryItemDto>?> GetItemsAsync(int page = 1, int pageSize = 15, string? search = null);
    Task<InventoryItemDto?> GetItemByIdAsync(Guid id);
    Task<InventoryItemDto?> CreateItemAsync(CreateInventoryItemRequest request);
    Task<InventoryItemDto?> UpdateItemAsync(Guid id, UpdateInventoryItemRequest request);
    Task<bool> DeleteItemAsync(Guid id);
}

public class InventoryService(HttpClient http, IAuthBridgeService auth) : IInventoryService
{
    private async Task SetAuthHeaderAsync()
    {
        var token = await auth.GetTokenAsync();
        if (!string.IsNullOrEmpty(token))
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public async Task<PagedResult<InventoryItemDto>?> GetItemsAsync(int page = 1, int pageSize = 15, string? search = null)
    {
        await SetAuthHeaderAsync();
        var qs = $"?pageNumber={page}&pageSize={pageSize}";
        if (!string.IsNullOrEmpty(search)) qs += $"&searchTerm={Uri.EscapeDataString(search)}";
        return await http.GetFromJsonAsync<PagedResult<InventoryItemDto>>($"/api/proxy/inventory{qs}");
    }

    public async Task<InventoryItemDto?> GetItemByIdAsync(Guid id)
    {
        await SetAuthHeaderAsync();
        return await http.GetFromJsonAsync<InventoryItemDto>($"/api/proxy/inventory/{id}");
    }

    public async Task<InventoryItemDto?> CreateItemAsync(CreateInventoryItemRequest request)
    {
        await SetAuthHeaderAsync();
        var response = await http.PostAsJsonAsync("/api/proxy/inventory", request);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<InventoryItemDto>();
    }

    public async Task<InventoryItemDto?> UpdateItemAsync(Guid id, UpdateInventoryItemRequest request)
    {
        await SetAuthHeaderAsync();
        var response = await http.PutAsJsonAsync($"/api/proxy/inventory/{id}", request);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<InventoryItemDto>();
    }

    public async Task<bool> DeleteItemAsync(Guid id)
    {
        await SetAuthHeaderAsync();
        var response = await http.DeleteAsync($"/api/proxy/inventory/{id}");
        return response.IsSuccessStatusCode;
    }
}
