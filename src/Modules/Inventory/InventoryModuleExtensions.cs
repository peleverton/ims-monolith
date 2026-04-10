using System.Data;
using Dapper;
using IMS.Modular.Modules.Inventory.Domain;
using IMS.Modular.Modules.Inventory.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

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

    /// <summary>
    /// Registers all Inventory module services: DbContext, Write repos (EF Core), Read repos (Dapper).
    /// </summary>
    public static IServiceCollection AddInventoryModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")!;

        // EF Core — write side
        services.AddDbContext<InventoryDbContext>(options =>
            options.UseSqlite(
                connectionString,
                b => b.MigrationsAssembly(typeof(InventoryDbContext).Assembly.FullName)));

        // Write repositories (EF Core)
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<ISupplierRepository, SupplierRepository>();
        services.AddScoped<ILocationRepository, LocationRepository>();
        services.AddScoped<IStockMovementRepository, StockMovementRepository>();

        // Dapper — read side (IDbConnection per request)
        services.AddScoped<IDbConnection>(_ => new SqliteConnection(connectionString));

        // Read repositories (Dapper)
        services.AddScoped<IProductReadRepository, ProductReadRepository>();
        services.AddScoped<IStockMovementReadRepository, StockMovementReadRepository>();
        services.AddScoped<ISupplierReadRepository, SupplierReadRepository>();
        services.AddScoped<ILocationReadRepository, LocationReadRepository>();

        return services;
    }

    /// <summary>
    /// Creates Inventory module tables if they don't exist (safe for shared SQLite file).
    /// </summary>
    public static async Task InitializeInventoryModuleAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();

        var sql = db.Database.GenerateCreateScript();

        foreach (var statement in sql.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var trimmed = statement.Trim();
            if (string.IsNullOrEmpty(trimmed))
                continue;

            try
            {
                await db.Database.ExecuteSqlRawAsync(trimmed);
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 1 && ex.Message.Contains("already exists"))
            {
                // Table/index already exists — safe to ignore
            }
        }
    }
}
