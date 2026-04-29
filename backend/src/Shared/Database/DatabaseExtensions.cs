using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace IMS.Modular.Shared.Database;

/// <summary>
/// US-024: Seleção condicional de provider de banco de dados.
/// US-065: ApplyMigrationsAsync — usa MigrateAsync em todos os providers reais;
///         EnsureCreated apenas para InMemory (testes de integração).
/// </summary>
public static class DatabaseExtensions
{
    /// <summary>
    /// Configura o DbContextOptionsBuilder com o provider correto para o ambiente.
    /// </summary>
    public static DbContextOptionsBuilder UseImsDatabase(
        this DbContextOptionsBuilder options,
        IConfiguration configuration,
        IWebHostEnvironment environment,
        string? migrationsAssembly = null)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");

        if (environment.IsDevelopment())
        {
            options.UseSqlite(connectionString,
                b =>
                {
                    if (migrationsAssembly is not null)
                        b.MigrationsAssembly(migrationsAssembly);
                });
        }
        else
        {
            options.UseNpgsql(connectionString,
                b =>
                {
                    if (migrationsAssembly is not null)
                        b.MigrationsAssembly(migrationsAssembly);

                    b.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorCodesToAdd: null);
                });
        }

        return options;
    }

    /// <summary>
    /// Retorna true se o ambiente usa PostgreSQL.
    /// </summary>
    public static bool IsPostgres(IWebHostEnvironment environment)
        => !environment.IsDevelopment();

    /// <summary>
    /// US-065: Aplica migrations ou EnsureCreated de acordo com o provider:
    /// - InMemory  → EnsureCreated (testes de integração)
    /// - SQLite    → EnsureCreated (desenvolvimento local, zero infra)
    /// - PostgreSQL → MigrateAsync (staging/produção — zero-downtime deploy)
    /// </summary>
    public static async Task ApplyMigrationsAsync<TContext>(this IServiceProvider services)
        where TContext : DbContext
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TContext>();

        var provider = db.Database.ProviderName ?? "";

        if (provider.Contains("InMemory", StringComparison.OrdinalIgnoreCase) ||
            provider.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
        {
            // Desenvolvimento e testes: EnsureCreated (rápido, sem migrations necessárias).
            // When sharing one SQLite file across multiple DbContexts, EnsureCreated is
            // a no-op for the second+ context because the file already exists.
            // CreateTables ensures THIS context's tables are created even when the DB file exists.
            var created = await db.Database.EnsureCreatedAsync();
            if (!created)
            {
                // DB file already exists — ensure this context's tables exist too.
                try
                {
                    var creator = (IRelationalDatabaseCreator)db.Database
                        .GetInfrastructure()
                        .GetRequiredService(typeof(IDatabaseCreator));
                    await creator.CreateTablesAsync();
                }
                catch { /* Tables may already exist; ignore "already exists" errors */ }
            }
            return;
        }

        // PostgreSQL (staging/prod): aplicar migrations versionadas
        await db.Database.MigrateAsync();
    }
}
