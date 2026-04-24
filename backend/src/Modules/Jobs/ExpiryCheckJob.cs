using IMS.Modular.Modules.Auth.Infrastructure;
using IMS.Modular.Modules.Inventory.Infrastructure;
using IMS.Modular.Modules.Issues.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace IMS.Modular.Modules.Jobs;

/// <summary>
/// US-067: Expiração de produtos — cria InventoryIssue para produtos
/// com validade nos próximos 30 dias.
/// </summary>
public class ExpiryCheckJob(InventoryDbContext inventory, ILogger<ExpiryCheckJob> logger)
{
    public async Task ExecuteAsync()
    {
        var threshold = DateTime.UtcNow.AddDays(30);
        var expiring = await inventory.Products
            .Where(p => p.ExpiryDate != null && p.ExpiryDate <= threshold && p.CurrentStock > 0)
            .ToListAsync();

        if (expiring.Count == 0)
        {
            logger.LogInformation("[ExpiryCheckJob] No expiring products found");
            return;
        }

        logger.LogInformation("[ExpiryCheckJob] Found {Count} expiring products", expiring.Count);
        // TODO (US-066): publicar evento para NotificationsModule criar InventoryIssue
        foreach (var product in expiring)
            logger.LogWarning("[ExpiryCheckJob] Product {Id} '{Name}' expires on {Date}",
                product.Id, product.Name, product.ExpiryDate);
    }
}
