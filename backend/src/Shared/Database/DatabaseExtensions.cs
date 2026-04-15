using Microsoft.EntityFrameworkCore;

namespace IMS.Modular.Shared.Database;

/// <summary>
/// US-024: Seleção condicional de provider de banco de dados.
///
/// Regra:
///   - Development  → SQLite  (zero infraestrutura, arquivo local)
///   - Staging/Prod → PostgreSQL (via "DefaultConnection" no appsettings/env var)
///
/// Uso nos módulos:
///   services.AddDbContext&lt;MyDbContext&gt;(opt =>
///       opt.UseImsDatabase(configuration, environment,
///           sqliteMigrations: typeof(MyDbContext).Assembly.FullName,
///           npgsqlMigrations: typeof(MyDbContext).Assembly.FullName));
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
}
