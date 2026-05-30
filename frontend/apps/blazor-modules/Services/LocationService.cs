using System.Net.Http.Headers;
using System.Net.Http.Json;
using BlazorModules.Models;

namespace BlazorModules.Services;

public interface ILocationService
{
    Task<List<LocationDto>> GetAllLocationsAsync();
}

public class LocationService(HttpClient http, IAuthBridgeService auth) : ILocationService
{
    private async Task SetAuthHeaderAsync()
    {
        var token = await auth.GetTokenAsync();
        if (!string.IsNullOrEmpty(token))
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public async Task<List<LocationDto>> GetAllLocationsAsync()
    {
        await SetAuthHeaderAsync();

        // Fetch all locations (paginated — get a large page to build the tree)
        var result = await http.GetFromJsonAsync<PagedResult<LocationDto>>(
            "/api/proxy/inventory/locations?pageNumber=1&pageSize=500");

        return result?.Items ?? [];
    }
}
