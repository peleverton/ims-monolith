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

    public static async Task InitializeInventoryModuleAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();

        if (db.Database.ProviderName?.Contains("InMemory", StringComparison.OrdinalIgnoreCase) == true)
        {
            await db.Database.EnsureCreatedAsync();
            return;
        }

        var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
        if (env.IsDevelopment())
        {
            var sql = db.Database.GenerateCreateScript();
            foreach (var statement in sql.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var trimmed = statement.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;
                try { await db.Database.ExecuteSqlRawAsync(trimmed); }
                catch (SqliteException ex)
                    when (ex.SqliteErrorCode == 1 && ex.Message.Contains("already exists")) { }
            }
        }
        else
        {
            var script = db.Database.GenerateCreateScript();
            foreach (var statement in script.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var trimmed = statement.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;
                try { await db.Database.ExecuteSqlRawAsync(trimmed); }
                catch (NpgsqlException ex) when (ex is Npgsql.PostgresException pe && (pe.SqlState == "42P07" || pe.SqlState == "42710" || pe.SqlState == "23505")) { }
            }
        }
    }
}
