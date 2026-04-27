using IMS.Modular.Modules.Search.Application;
using IMS.Modular.Modules.Search.Infrastructure;
using Meilisearch;

namespace IMS.Modular.Modules.Search;

/// <summary>US-071: SearchModule DI registration.</summary>
public static class SearchModuleExtensions
{
    public static IServiceCollection AddSearchModule(this IServiceCollection services, IConfiguration configuration)
    {
        var host = configuration["Meilisearch:Host"] ?? "http://meilisearch:7700";
        var apiKey = configuration["Meilisearch:ApiKey"] ?? "";

        services.AddSingleton(_ => new MeilisearchClient(host, apiKey));
        services.AddScoped<ISearchService, MeilisearchService>();

        return services;
    }

    /// <summary>Ensure Meilisearch indexes exist with correct settings.</summary>
    public static async Task InitializeSearchModuleAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var client = scope.ServiceProvider.GetRequiredService<MeilisearchClient>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<MeilisearchClient>>();

        var indexes = new[]
        {
            (Name: MeilisearchService.IssuesIndex,    PrimaryKey: "id"),
            (Name: MeilisearchService.InventoryIndex, PrimaryKey: "id"),
        };

        foreach (var (name, pk) in indexes)
        {
            try
            {
                await client.CreateIndexAsync(name, pk);
                logger.LogInformation("[Search] Index '{Index}' ensured", name);
            }
            catch (Exception ex)
            {
                // Index may already exist — log warning and continue
                logger.LogWarning("[Search] Could not create index '{Index}': {Msg}", name, ex.Message);
            }
        }
    }
}
