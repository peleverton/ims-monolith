using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.FeatureManagement;
using IMS.Modular.Modules.Inventory.Infrastructure;
using IMS.Modular.Modules.Inventory.Domain.Entities;
using IMS.Modular.Modules.Inventory.Domain.Enums;
using IMS.Modular.Modules.Issues.Infrastructure;
using IMS.Modular.Modules.Issues.Domain.Entities;
using IMS.Modular.Modules.Issues.Domain.Enums;
using IMS.Modular.Modules.Auth.Infrastructure;
using IMS.Modular.Modules.Auth.Domain.Entities;
using IMS.Modular.Modules.InventoryIssues.Infrastructure;
using IMS.Modular.Modules.Webhooks.Infrastructure;
using IMS.Modular.Shared.MultiTenancy.TenantManagement;
using IMS.Modular.Shared.Outbox;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.Data;
using Microsoft.Data.Sqlite;
using Xunit;

namespace IMS.Modular.Tests.Integration;

// ── Feature manager stub that enables ONLY EnableMultiTenancy ───────────────
internal sealed class MultiTenancyEnabledFeatureManager : IFeatureManager
{
    public IAsyncEnumerable<string> GetFeatureNamesAsync() => GetEmpty();
    private static async IAsyncEnumerable<string> GetEmpty() { await Task.CompletedTask; yield break; }
    public Task<bool> IsEnabledAsync(string feature)
        => Task.FromResult(feature == IMS.Modular.Shared.FeatureFlags.FeatureFlags.EnableMultiTenancy);
    public Task<bool> IsEnabledAsync<TContext>(string feature, TContext context)
        => Task.FromResult(feature == IMS.Modular.Shared.FeatureFlags.FeatureFlags.EnableMultiTenancy);
}

// ── xUnit collection for multi-tenancy tests ────────────────────────────────
[CollectionDefinition("MultiTenancy")]
public class MultiTenancyCollection : ICollectionFixture<MultiTenantWebAppFactory> { }

/// <summary>
/// US-080: WebApplicationFactory that enables EnableMultiTenancy and seeds data
/// for two tenants so TenantIsolationIntegrationTests can assert real data isolation.
/// </summary>
public class MultiTenantWebAppFactory : WebApplicationFactory<Program>, IDisposable
{
    private readonly string _tempDir;
    private readonly string _sharedConnStr;
    private readonly string _outboxConnStr;
    private readonly string _webhooksConnStr;

    // Two demo tenants seeded in inventory/issues
    public const string TenantAlpha = "tenant-demo-1";
    public const string TenantBeta  = "tenant-demo-2";

    // Seeded product IDs per tenant (so tests can verify strict isolation)
    public static readonly Guid AlphaProductId = Guid.Parse("a0000000-0000-0000-0000-000000000001");
    public static readonly Guid BetaProductId  = Guid.Parse("b0000000-0000-0000-0000-000000000001");
    public static readonly Guid AlphaIssueId   = Guid.Parse("a0000000-0000-0000-0000-000000000002");
    public static readonly Guid BetaIssueId    = Guid.Parse("b0000000-0000-0000-0000-000000000002");

    static MultiTenantWebAppFactory()
        => Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");

    public MultiTenantWebAppFactory()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
        _tempDir = Path.Combine(Path.GetTempPath(), $"ims-mt-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        _sharedConnStr  = $"Data Source={_tempDir}/shared.db;Cache=Shared";
        _outboxConnStr  = $"Data Source={_tempDir}/outbox.db";
        _webhooksConnStr = $"Data Source={_tempDir}/webhooks.db";
    }

    public string AdminToken { get; private set; } = string.Empty;

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _sharedConnStr,
                ["IntegrationTestMode"] = "true",
                ["ConnectionStrings:Redis"] = "",
                ["RabbitMQ:Host"] = "",
                ["Outbox:PollingIntervalSeconds"] = "3600",
                ["RateLimiting:Auth:PermitLimit"] = "10000",
                ["RateLimiting:Auth:WindowSeconds"] = "1",
                ["RateLimiting:Global:PermitLimit"] = "10000",
                ["RateLimiting:Global:WindowSeconds"] = "1",
            });
        });

        builder.ConfigureServices(services =>
        {
            ReplaceDb<AuthDbContext>(services, _sharedConnStr);
            ReplaceDb<InventoryDbContext>(services, _sharedConnStr);
            ReplaceDb<IssuesDbContext>(services, _sharedConnStr);
            ReplaceDb<InventoryIssuesDbContext>(services, _sharedConnStr);
            ReplaceDb<OutboxDbContext>(services, _outboxConnStr);
            ReplaceDb<WebhooksDbContext>(services, _webhooksConnStr);
            ReplaceDb<TenantDbContext>(services, _sharedConnStr);

            services.RemoveAll<IDbConnection>();
            services.AddScoped<IDbConnection>(_ => new SqliteConnection(_sharedConnStr));

            // Enable multi-tenancy feature flag
            services.RemoveAll<IFeatureManager>();
            services.AddSingleton<IFeatureManager>(new MultiTenancyEnabledFeatureManager());
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        SeedAll(host.Services);
        return host;
    }

    private static void ReplaceDb<TContext>(IServiceCollection services, string connStr)
        where TContext : DbContext
    {
        var toRemove = services
            .Where(s => s.ServiceType == typeof(DbContextOptions<TContext>)
                     || s.ServiceType == typeof(TContext)
                     || (s.ServiceType.IsGenericType
                         && s.ServiceType.GetGenericTypeDefinition().FullName?
                             .Contains("IDbContextOptionsConfiguration") == true
                         && s.ServiceType.GenericTypeArguments.FirstOrDefault() == typeof(TContext)))
            .ToList();
        foreach (var d in toRemove) services.Remove(d);
        services.AddDbContext<TContext>((_, opt) => opt.UseSqlite(connStr));
    }

    // ── Seed ─────────────────────────────────────────────────────────────────

    private static void SeedAll(IServiceProvider sp)
    {
        EnsureSchema<AuthDbContext>(sp);
        EnsureSchema<InventoryDbContext>(sp);
        EnsureSchema<IssuesDbContext>(sp);
        EnsureSchema<InventoryIssuesDbContext>(sp);
        EnsureSchema<TenantDbContext>(sp);
        EnsureSchema<OutboxDbContext>(sp);
        EnsureSchema<WebhooksDbContext>(sp);

        SeedAuth(sp);
        SeedTenants(sp);
        SeedInventoryPerTenant(sp);
        SeedIssuesPerTenant(sp);
    }

    private static void EnsureSchema<TContext>(IServiceProvider sp) where TContext : DbContext
    {
        try
        {
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TContext>();
            db.Database.EnsureCreated();
        }
        catch (Exception ex) { Console.Error.WriteLine($"[MT-Schema:{typeof(TContext).Name}] {ex.Message}"); }
    }

    private static void SeedAuth(IServiceProvider sp)
    {
        try
        {
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
            if (db.Users.Any(u => u.Username == "admin")) return;

            var hash = Convert.ToBase64String(
                System.Security.Cryptography.SHA256.HashData(
                    System.Text.Encoding.UTF8.GetBytes("Admin@123!")));

            var roleId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            if (!db.Roles.Any(r => r.Id == roleId))
                db.Roles.Add(new Role { Id = roleId, Name = "Admin", Description = "Administrator" });

            var userId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
            var user = new User
            {
                Id = userId, Email = "admin@ims.local", Username = "admin",
                PasswordHash = hash, FullName = "MT Admin", IsActive = true, CreatedAt = DateTime.UtcNow
            };
            db.Users.Add(user);
            db.UserRoles.Add(new UserRole { UserId = userId, RoleId = roleId });
            db.SaveChanges();
        }
        catch (Exception ex) { Console.Error.WriteLine($"[MT-Seed:Auth] {ex.Message}"); }
    }

    private static void SeedTenants(IServiceProvider sp)
    {
        try
        {
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TenantDbContext>();
            if (!db.Tenants.Any())
            {
                db.Tenants.AddRange(
                    new TenantEntity { Id = "default", Name = "Default", Plan = "pro", IsActive = true, CreatedAt = DateTime.UtcNow },
                    new TenantEntity { Id = TenantAlpha, Name = "Alpha Corp", Plan = "starter", IsActive = true, CreatedAt = DateTime.UtcNow },
                    new TenantEntity { Id = TenantBeta,  Name = "Beta Corp",  Plan = "free",    IsActive = true, CreatedAt = DateTime.UtcNow }
                );
                db.SaveChanges();
            }
        }
        catch (Exception ex) { Console.Error.WriteLine($"[MT-Seed:Tenants] {ex.Message}"); }
    }

    private static void SeedInventoryPerTenant(IServiceProvider sp)
    {
        try
        {
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();

            // Manually bypass EF tenant filter for seeding
            if (db.Products.IgnoreQueryFilters().Any(p => p.Id == AlphaProductId)) return;

            var alpha = new Product(
                "Alpha Laptop", "ALPHA-LT-01", ProductCategory.Electronics,
                minimumStockLevel: 2, maximumStockLevel: 50,
                unitPrice: 3000m, costPrice: 2000m);
            SetId(alpha, AlphaProductId);
            alpha.TenantId = TenantAlpha;

            var beta = new Product(
                "Beta Tablet", "BETA-TB-01", ProductCategory.Electronics,
                minimumStockLevel: 5, maximumStockLevel: 100,
                unitPrice: 1500m, costPrice: 900m);
            SetId(beta, BetaProductId);
            beta.TenantId = TenantBeta;

            db.Products.AddRange(alpha, beta);
            db.SaveChanges();
        }
        catch (Exception ex) { Console.Error.WriteLine($"[MT-Seed:Inventory] {ex.Message}"); }
    }

    private static void SeedIssuesPerTenant(IServiceProvider sp)
    {
        try
        {
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IssuesDbContext>();
            var adminId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

            if (db.Issues.IgnoreQueryFilters().Any(i => i.Id == AlphaIssueId)) return;

            var alphaIssue = new Issue("Alpha Bug", "Only visible to alpha", IssuePriority.High, adminId);
            SetId(alphaIssue, AlphaIssueId);
            alphaIssue.TenantId = TenantAlpha;

            var betaIssue = new Issue("Beta Feature", "Only visible to beta", IssuePriority.Low, adminId);
            SetId(betaIssue, BetaIssueId);
            betaIssue.TenantId = TenantBeta;

            db.Issues.AddRange(alphaIssue, betaIssue);
            db.SaveChanges();
        }
        catch (Exception ex) { Console.Error.WriteLine($"[MT-Seed:Issues] {ex.Message}"); }
    }

    private static void SetId<T>(T entity, Guid id)
        => typeof(T).GetProperty("Id")!.SetValue(entity, id);

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
        }
        catch (Exception ex) { Console.Error.WriteLine($"[MT-Factory] token: {ex.Message}"); }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            try { Directory.Delete(_tempDir, recursive: true); } catch { }
        base.Dispose(disposing);
    }
}
