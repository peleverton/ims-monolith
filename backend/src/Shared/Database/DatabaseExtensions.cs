using Microsoft.EntityFrameworkCore;

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
    /// US-065: Aplica migrations (SQLite e PostgreSQL) ou EnsureCreated (InMemory/testes).
    /// Deve ser chamado no startup em substituição ao GenerateCreateScript manual.
    /// </summary>
    public static async Task ApplyMigrationsAsync<TContext>(this IServiceProvider services)
        where TContext : DbContext
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TContext>();

        // InMemory: não há migrations — EnsureCreated é suficiente
        if (db.Database.ProviderName?.Contains("InMemory", StringComparison.OrdinalIgnoreCase) == true)
        {
            await db.Database.EnsureCreatedAsync();
            return;
        }

        // SQLite (desenvolvimento) e PostgreSQL (produção): aplicar migrations versionadas
        await db.Database.MigrateAsync();
    }
}
