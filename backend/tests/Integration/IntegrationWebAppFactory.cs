using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using IMS.Modular.Modules.Inventory.Infrastructure;
using IMS.Modular.Modules.Inventory.Domain.Entities;
using IMS.Modular.Modules.Inventory.Domain.Enums;
using IMS.Modular.Modules.Issues.Infrastructure;
using IMS.Modular.Modules.Issues.Domain.Entities;
using IMS.Modular.Modules.Issues.Domain.Enums;
using IMS.Modular.Modules.Auth.Infrastructure;
using IMS.Modular.Modules.Auth.Domain.Entities;
using Microsoft.Extensions.DependencyInjection.Extensions;
using IMS.Modular.Modules.InventoryIssues.Infrastructure;
using IMS.Modular.Modules.Webhooks.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using IMS.Modular.Shared.Outbox;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.Net.Http.Json;
using System.Text.Json;
using System.Data;
using Microsoft.Data.Sqlite;
using Microsoft.FeatureManagement;

namespace IMS.Modular.Tests.Integration;

/// <summary>
/// IFeatureManager stub that disables ALL feature flags.
/// Used in integration tests so TenantAwareDbContext skips tenant filtering,
/// letting tests query all data without needing a tenant header.
/// </summary>
internal sealed class DisabledFeatureManager : IFeatureManager
{
    public IAsyncEnumerable<string> GetFeatureNamesAsync() => GetEmpty();
    private static async IAsyncEnumerable<string> GetEmpty() { await Task.CompletedTask; yield break; }
    public Task<bool> IsEnabledAsync(string feature) => Task.FromResult(false);
    public Task<bool> IsEnabledAsync<TContext>(string feature, TContext context) => Task.FromResult(false);
}

/// <summary>
/// Shared xUnit collection so all integration tests share ONE IntegrationWebAppFactory instance.
/// This prevents the race condition from multiple factories being created in parallel.
/// </summary>
[CollectionDefinition("Integration")]
public class IntegrationCollection : ICollectionFixture<IntegrationWebAppFactory> { }

/// <summary>
/// IDs fixos usados pelo seed de integração — permite que os testes referenciem
/// entidades pré-criadas sem fazer chamadas extras à API.
/// </summary>
public static class IntegrationSeedData
{
    // Auth
    public static readonly Guid AdminUserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    public static readonly Guid AdminRoleId  = Guid.Parse("11111111-1111-1111-1111-111111111111");

    // Inventory — Locais
    public static readonly Guid WarehouseAId   = Guid.Parse("10000000-0000-0000-0000-000000000001");
    public static readonly Guid ShelfA1Id      = Guid.Parse("10000000-0000-0000-0000-000000000002");

    // Inventory — Fornecedores
    public static readonly Guid SupplierTechId = Guid.Parse("20000000-0000-0000-0000-000000000001");
    public static readonly Guid SupplierFoodId = Guid.Parse("20000000-0000-0000-0000-000000000002");

    // Inventory — Produtos
    public static readonly Guid ProductLaptopId  = Guid.Parse("30000000-0000-0000-0000-000000000001");
    public static readonly Guid ProductMouseId   = Guid.Parse("30000000-0000-0000-0000-000000000002");
    public static readonly Guid ProductCoffeeId  = Guid.Parse("30000000-0000-0000-0000-000000000003");

    // Issues
    public static readonly Guid IssueBugId       = Guid.Parse("40000000-0000-0000-0000-000000000001");
    public static readonly Guid IssueFeatureId   = Guid.Parse("40000000-0000-0000-0000-000000000002");
}

/// <summary>
/// WebApplicationFactory that uses per-module temporary SQLite files so that
/// EnsureCreated works correctly for each module, and Dapper reads from the same file.
/// </summary>
public class IntegrationWebAppFactory : WebApplicationFactory<Program>, IDisposable
{
    private readonly string _tempDir;
    private readonly string _sharedConnStr;
    private readonly string _outboxConnStr;
    private readonly string _webhooksConnStr;

    static IntegrationWebAppFactory()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
    }

    public IntegrationWebAppFactory()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");

        _tempDir = Path.Combine(Path.GetTempPath(), $"ims-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);

        _sharedConnStr          = $"Data Source={_tempDir}/shared.db;Cache=Shared";
        _outboxConnStr          = $"Data Source={_tempDir}/outbox.db";
        _webhooksConnStr        = $"Data Source={_tempDir}/webhooks.db";
    }

    /// <summary>Cached admin token obtained once after factory starts.</summary>
    public string AdminToken { get; private set; } = string.Empty;

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                // Use the Inventory SQLite file for DefaultConnection (used by Dapper)
                ["ConnectionStrings:DefaultConnection"] = _sharedConnStr,
                ["IntegrationTestMode"] = "true",
                ["ConnectionStrings:Redis"] = "",
                ["RabbitMQ:Host"] = "",
                ["Outbox:PollingIntervalSeconds"] = "3600",
                ["RateLimiting:Auth:PermitLimit"] = "10000",
                ["RateLimiting:Auth:WindowSeconds"] = "1",
                ["RateLimiting:Global:PermitLimit"] = "10000",
                ["RateLimiting:Global:WindowSeconds"] = "1"
            });
        });

        builder.ConfigureServices(services =>
        {
            ReplaceDbContextWithSqlite<AuthDbContext>(services, _sharedConnStr);
            ReplaceDbContextWithSqlite<InventoryDbContext>(services, _sharedConnStr);
            ReplaceDbContextWithSqlite<IssuesDbContext>(services, _sharedConnStr);
            ReplaceDbContextWithSqlite<InventoryIssuesDbContext>(services, _sharedConnStr);
            ReplaceDbContextWithSqlite<OutboxDbContext>(services, _outboxConnStr);
            ReplaceDbContextWithSqlite<WebhooksDbContext>(services, _webhooksConnStr);

            // Replace Dapper IDbConnection to use the Inventory SQLite file
            services.RemoveAll<IDbConnection>();
            services.AddScoped<IDbConnection>(_ => new SqliteConnection(_sharedConnStr));

            // US-081: Ensure IFeatureManager is available for TenantAwareDbContext.
            // Multi-tenancy is disabled in tests — all queries return unrestricted data.
            services.RemoveAll<IFeatureManager>();
            services.AddSingleton<IFeatureManager>(new DisabledFeatureManager());
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        SeedAllData(host.Services);
        return host;
    }

    /// <summary>
    /// Obtains and caches the admin token. Call this once from the test's InitializeAsync.
    /// </summary>
    public async Task EnsureAdminTokenAsync()
    {
        if (!string.IsNullOrEmpty(AdminToken)) return;
        try
        {
            using var client = CreateClient();
            var response = await client.PostAsJsonAsync("/api/auth/login", new
            {
                username = "admin",
                password = "Admin@123!"
            });

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadFromJsonAsync<JsonElement>();
                AdminToken = json.GetProperty("accessToken").GetString() ?? string.Empty;
            }
            else
            {
                var body = await response.Content.ReadAsStringAsync();
                Console.Error.WriteLine($"[IntegrationWebAppFactory] Login failed ({response.StatusCode}): {body}");
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[IntegrationWebAppFactory] EnsureAdminTokenAsync failed: {ex.Message}");
        }
    }

    private static void ReplaceDbContextWithSqlite<TContext>(IServiceCollection services, string connectionString)
        where TContext : DbContext
    {
        var descriptorsToRemove = services
            .Where(s => s.ServiceType == typeof(DbContextOptions<TContext>)
                     || s.ServiceType == typeof(TContext)
                     || (s.ServiceType.IsGenericType
                         && s.ServiceType.GetGenericTypeDefinition().FullName?
                             .Contains("IDbContextOptionsConfiguration") == true
                         && s.ServiceType.GenericTypeArguments.FirstOrDefault() == typeof(TContext)))
            .ToList();
        foreach (var d in descriptorsToRemove) services.Remove(d);

        services.AddDbContext<TContext>((_, options) => options.UseSqlite(connectionString));
    }

    // ── Seed ─────────────────────────────────────────────────────────────────

    private static void SeedAllData(IServiceProvider services)
    {
        // Ensure all DbContext schemas are created in the shared SQLite file.
        // EnsureCreated only works for the first context; subsequent contexts
        // need CreateTables() to create their tables in the existing DB file.
        EnsureSchemas(services);
        SeedAuthData(services);
        SeedInventoryData(services);
        SeedIssuesData(services);
    }

    private static void EnsureSchemas(IServiceProvider services)
    {
        EnsureSchema<AuthDbContext>(services);
        EnsureSchema<InventoryDbContext>(services);
        EnsureSchema<IssuesDbContext>(services);
        EnsureSchema<InventoryIssuesDbContext>(services);
        EnsureSchema<OutboxDbContext>(services);
        EnsureSchema<WebhooksDbContext>(services);
    }

    private static void EnsureSchema<TContext>(IServiceProvider services) where TContext : DbContext
    {
        try
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TContext>();
            var creator = db.GetService<IRelationalDatabaseCreator>();
            creator.EnsureCreated();
            try { creator.CreateTables(); } catch { /* tables may already exist */ }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[Schema:{typeof(TContext).Name}] {ex.Message}");
        }
    }

    /// <summary>
    /// Seed Auth: admin user + Admin role.
    /// HasData do AuthDbContext já cria via EnsureCreated; este método é fallback.
    /// </summary>
    private static void SeedAuthData(IServiceProvider services)
    {
        try
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
            db.Database.EnsureCreated();

            if (!db.Users.Any(u => u.Username == "admin"))
            {
                var hash = Convert.ToBase64String(
                    System.Security.Cryptography.SHA256.HashData(
                        System.Text.Encoding.UTF8.GetBytes("Admin@123!")));

                if (!db.Roles.Any(r => r.Id == IntegrationSeedData.AdminRoleId))
                    db.Roles.Add(new Role
                    {
                        Id = IntegrationSeedData.AdminRoleId,
                        Name = "Admin",
                        Description = "Administrator"
                    });

                var user = new User
                {
                    Id = IntegrationSeedData.AdminUserId,
                    Email = "admin@ims.local",
                    Username = "admin",
                    PasswordHash = hash,
                    FullName = "Integration Admin",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                db.Users.Add(user);
                db.UserRoles.Add(new UserRole
                {
                    UserId = user.Id,
                    RoleId = IntegrationSeedData.AdminRoleId
                });
                db.SaveChanges();
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[Seed:Auth] {ex.Message}");
        }
    }

    /// <summary>
    /// Seed Inventory: 2 locais, 2 fornecedores e 3 produtos com estoque inicial.
    /// Produtos: Laptop (normal), Mouse (ok), Café (abaixo do mínimo → LowStock).
    /// Always removes any pre-existing data (e.g., demo tenant data) before seeding.
    /// </summary>
    private static void SeedInventoryData(IServiceProvider services)
    {
        try
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
            db.Database.EnsureCreated();

            // Remove any demo/pre-existing data to ensure only integration seed is present
            if (db.Products.Any())
            {
                db.StockMovements.RemoveRange(db.StockMovements);
                db.Products.RemoveRange(db.Products);
                db.Suppliers.RemoveRange(db.Suppliers);
                db.Locations.RemoveRange(db.Locations);
                db.SaveChanges();
            }

            // --- Locais ---
            var warehouse = new Location("Armazém Central", "WH-A",
                LocationType.Warehouse, capacity: 1000);
            SetId(warehouse, IntegrationSeedData.WarehouseAId);

            var shelf = new Location("Prateleira A1", "SHELF-A1",
                LocationType.Shelf, capacity: 200,
                parentLocationId: IntegrationSeedData.WarehouseAId);
            SetId(shelf, IntegrationSeedData.ShelfA1Id);

            db.Locations.AddRange(warehouse, shelf);

            // --- Fornecedores ---
            var supplierTech = new Supplier("TechSupply Ltda", "TECH-001",
                contactPerson: "João Silva", email: "joao@techsupply.com");
            SetId(supplierTech, IntegrationSeedData.SupplierTechId);

            var supplierFood = new Supplier("FoodDist S.A.", "FOOD-001",
                contactPerson: "Maria Costa", email: "maria@fooddist.com");
            SetId(supplierFood, IntegrationSeedData.SupplierFoodId);

            db.Suppliers.AddRange(supplierTech, supplierFood);

            // --- Produtos ---
            var laptop = new Product(
                "Notebook ProBook 450", "NB-PRO-450", ProductCategory.Electronics,
                minimumStockLevel: 5, maximumStockLevel: 50,
                unitPrice: 3499.99m, costPrice: 2800.00m,
                description: "Notebook empresarial 15.6\"");
            SetId(laptop, IntegrationSeedData.ProductLaptopId);
            laptop.SetLocation(IntegrationSeedData.ShelfA1Id);
            laptop.SetSupplier(IntegrationSeedData.SupplierTechId);
            laptop.AdjustStock(20, StockMovementType.InitialStock); // estoque normal

            var mouse = new Product(
                "Mouse Óptico USB", "MOUSE-USB-01", ProductCategory.Electronics,
                minimumStockLevel: 10, maximumStockLevel: 200,
                unitPrice: 49.90m, costPrice: 25.00m);
            SetId(mouse, IntegrationSeedData.ProductMouseId);
            mouse.SetLocation(IntegrationSeedData.ShelfA1Id);
            mouse.SetSupplier(IntegrationSeedData.SupplierTechId);
            mouse.AdjustStock(150, StockMovementType.InitialStock); // estoque alto

            var coffee = new Product(
                "Café Torrado 500g", "CAFE-500G", ProductCategory.Food,
                minimumStockLevel: 20, maximumStockLevel: 500,
                unitPrice: 18.90m, costPrice: 10.00m,
                unit: "kg");
            SetId(coffee, IntegrationSeedData.ProductCoffeeId);
            coffee.SetSupplier(IntegrationSeedData.SupplierFoodId);
            coffee.AdjustStock(3, StockMovementType.InitialStock); // abaixo do mínimo → LowStock

            db.Products.AddRange(laptop, mouse, coffee);
            db.SaveChanges();

            // Explicitly create StockMovement records (AdjustStock only raises domain events)
            db.StockMovements.AddRange(
                new StockMovement(laptop.Id, StockMovementType.InitialStock, 20,
                    IntegrationSeedData.ShelfA1Id),
                new StockMovement(mouse.Id, StockMovementType.InitialStock, 150,
                    IntegrationSeedData.ShelfA1Id),
                new StockMovement(coffee.Id, StockMovementType.InitialStock, 3)
            );
            db.SaveChanges();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[Seed:Inventory] {ex.Message}");
        }
    }

    /// <summary>
    /// Seed Issues: 1 bug (High) e 1 feature (Medium), ambas abertas.
    /// </summary>
    private static void SeedIssuesData(IServiceProvider services)
    {
        try
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IssuesDbContext>();
            db.Database.EnsureCreated();

            // Remove any pre-existing data before seeding
            if (db.Issues.Any())
            {
                db.Issues.RemoveRange(db.Issues);
                db.SaveChanges();
            }

            var bug = new Issue(
                "Falha ao importar planilha de produtos",
                "O sistema retorna erro 500 ao tentar importar arquivo .xlsx com mais de 500 linhas.",
                IssuePriority.High,
                reporterId: IntegrationSeedData.AdminUserId,
                dueDate: DateTime.UtcNow.AddDays(7));
            SetId(bug, IntegrationSeedData.IssueBugId);

            var feature = new Issue(
                "Implementar exportação de relatório em PDF",
                "Adicionar opção de exportar o relatório de estoque em formato PDF.",
                IssuePriority.Medium,
                reporterId: IntegrationSeedData.AdminUserId,
                dueDate: DateTime.UtcNow.AddDays(30));
            SetId(feature, IntegrationSeedData.IssueFeatureId);

            db.Issues.AddRange(bug, feature);
            db.SaveChanges();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[Seed:Issues] {ex.Message}");
        }
    }

    /// <summary>
    /// Atribui Id fixo a uma entidade (Id é gerado pelo construtor de BaseEntity).
    /// </summary>
    private static void SetId<T>(T entity, Guid id)
        => typeof(T).GetProperty("Id")!.SetValue(entity, id);

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            try { Directory.Delete(_tempDir, recursive: true); } catch { }
        }
        base.Dispose(disposing);
    }
}
