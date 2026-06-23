using IMS.Modular.Modules.BinPacking.Application.Services;
using IMS.Modular.Modules.BinPacking.Domain;
using IMS.Modular.Modules.BinPacking.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace IMS.Modular.Modules.BinPacking;

public static class BinPackingModuleExtensions
{
    public static IServiceCollection AddBinPackingModule(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        services.AddDbContext<BinPackingDbContext>((sp, options) =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection")!;
            if (connectionString.Contains(".db", StringComparison.OrdinalIgnoreCase))
                options.UseSqlite(connectionString);
            else
                options.UseNpgsql(connectionString);
        });

        services.AddScoped<IPackagingTypeRepository, PackagingTypeRepository>();
        services.AddSingleton<FirstFitDecreasingPacker>();

        return services;
    }

    public static async Task UseBinPackingModuleAsync(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<BinPackingDbContext>();
            await db.Database.EnsureCreatedAsync();
        }
    }
}
