using System.Data;
using Dapper;
using IMS.Modular.Modules.Inventory.Domain;
using IMS.Modular.Modules.Inventory.Infrastructure;
using IMS.Modular.Shared.Database;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace IMS.Modular.Modules.Inventory;

/// <summary>
/// Dapper type handler: maps SQLite TEXT ↔ System.Guid.
/// Required because SQLite stores GUIDs as TEXT, but Dapper expects the CLR Guid type.
/// </summary>
file sealed class GuidTypeHandler : SqlMapper.TypeHandler<Guid>
{
    public override void SetValue(IDbDataParameter parameter, Guid value)
        => parameter.Value = value.ToString().ToUpperInvariant();

    public override Guid Parse(object value)
        => Guid.Parse(value.ToString()!);
}

/// <summary>
/// DI registration and database initialization for the Inventory module.
/// US-024: conditional SQLite (dev) / PostgreSQL (prod) provider.
/// </summary>
public static class InventoryModuleExtensions
{
    // Register the Dapper type handler once (idempotent — safe to call multiple times).
    static InventoryModuleExtensions()
    {
        SqlMapper.RemoveTypeMap(typeof(Guid));
        SqlMapper.RemoveTypeMap(typeof(Guid?));
        SqlMapper.AddTypeHandler(new GuidTypeHandler());
    }

    public static IServiceCollection AddInventoryModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")!;

        // EF Core — write side (US-024: provider selected by environment)
        services.AddDbContext<InventoryDbContext>((sp, options) =>
        {
            var env = sp.GetRequiredService<IWebHostEnvironment>();
            options.UseImsDatabase(configuration, env,
                migrationsAssembly: typeof(InventoryDbContext).Assembly.FullName);
        });

        // Write repositories (EF Core)
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<ISupplierRepository, SupplierRepository>();
        services.AddScoped<ILocationRepository, LocationRepository>();
        services.AddScoped<IStockMovementRepository, StockMovementRepository>();

        // Dapper — read side: IDbConnection resolved by environment at runtime
        services.AddScoped<IDbConnection>(sp =>
        {
            var env = sp.GetRequiredService<IWebHostEnvironment>();
            if (env.IsDevelopment())
                return new SqliteConnection(connectionString);
            return new NpgsqlConnection(connectionString);
        });

        // Read repositories (Dapper)
        services.AddScoped<IProductReadRepository, ProductReadRepository>();
        services.AddScoped<IStockMovementReadRepository, StockMovementReadRepository>();
        services.AddScoped<ISupplierReadRepository, SupplierReadRepository>();
        services.AddScoped<ILocationReadRepository, LocationReadRepository>();

        return services;
    }

    /// <summary>
    /// US-065: Usa MigrateAsync (SQLite/PostgreSQL) ou EnsureCreated (InMemory/testes).
    /// US-081: After migrations, seeds demo data for tenant-demo-1 and tenant-demo-2.
    /// </summary>
    public static async Task InitializeInventoryModuleAsync(this IServiceProvider services)
    {
        await services.ApplyMigrationsAsync<InventoryDbContext>();

        using var scope = services.CreateScope();
        var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
        if (env.IsDevelopment())
        {
            // Skip tenant demo seed in integration test environments
            // (detected by the IntegrationTestMode configuration key)
            var config = scope.ServiceProvider.GetService<Microsoft.Extensions.Configuration.IConfiguration>();
            if (config?["IntegrationTestMode"] == "true")
                return;

            var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
            await InventoryTenantSeed.SeedAsync(db);
        }
    }
}
