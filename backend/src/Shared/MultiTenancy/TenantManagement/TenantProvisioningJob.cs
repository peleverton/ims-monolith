using Hangfire;
using IMS.Modular.Modules.Billing.Application.Services;
using IMS.Modular.Shared.Abstractions;

namespace IMS.Modular.Shared.MultiTenancy.TenantManagement;

/// <summary>
/// US-092: Hangfire background job that finalises new-tenant provisioning after signup.
///
/// Steps:
///   1. Set up billing subscription via IBillingService.
///   2. Send welcome email via IEmailService.
///   3. Mark TenantEntity.IsActive = true.
///
/// On failure: logs the error, sets IsActive = false and records an error note so the
/// platform operator can investigate.  Hangfire will retry up to 3 times automatically.
/// </summary>
public sealed class TenantProvisioningJob(
    TenantDbContext tenantDb,
    IBillingService billingService,
    IEmailService emailService,
    ILogger<TenantProvisioningJob> logger)
{
    [AutomaticRetry(Attempts = 3)]
    public async Task ExecuteAsync(string tenantId, string planId, string adminEmail)
    {
        logger.LogInformation(
            "[TenantProvisioning] Starting for tenantId={TenantId} plan={Plan}",
            tenantId, planId);

        var tenant = await tenantDb.Tenants.FindAsync(tenantId);
        if (tenant is null)
        {
            logger.LogError(
                "[TenantProvisioning] Tenant {TenantId} not found — skipping.", tenantId);
            return;
        }

        try
        {
            // 1. Create / retrieve billing subscription
            await billingService.GetOrCreateSubscriptionAsync(tenantId, planId);

            // 2. Send welcome email
            var subject = "Welcome to IMS — your account is ready!";
            var body = $"""
                <h2>Your organisation has been provisioned!</h2>
                <p>Tenant: <strong>{tenantId}</strong></p>
                <p>Plan: <strong>{planId}</strong></p>
                <p>You can now log in at <a href="https://app.ims.io">https://app.ims.io</a></p>
                """;
            await emailService.SendAsync(adminEmail, subject, body);

            // 3. Activate tenant
            tenant.IsActive = true;
            await tenantDb.SaveChangesAsync();

            logger.LogInformation(
                "[TenantProvisioning] Tenant {TenantId} provisioned and activated.", tenantId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "[TenantProvisioning] Failed to provision tenant {TenantId}.", tenantId);

            // Mark as failed so the operator can identify the broken tenants
            tenant.IsActive = false;
            tenant.Notes = $"Provisioning failed at {DateTime.UtcNow:O}: {ex.Message}";
            try { await tenantDb.SaveChangesAsync(); } catch { /* best-effort */ }

            throw; // allow Hangfire to retry
        }
    }
}
