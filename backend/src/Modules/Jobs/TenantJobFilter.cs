using Hangfire;
using Hangfire.Client;
using Hangfire.Common;
using Hangfire.Server;
using Hangfire.States;
using IMS.Modular.Shared.MultiTenancy;

namespace IMS.Modular.Modules.Jobs;

/// <summary>
/// US-086: Hangfire client + server filter that automatically tags jobs with
/// the current tenant when multi-tenancy is active.
/// - IClientFilter.OnCreating: adds "tenant:{tenantId}" parameter to the job before enqueueing
/// - IServerFilter.OnPerforming: reads tenant tag and sets it in context for job execution
/// </summary>
public sealed class TenantJobFilter : JobFilterAttribute, IClientFilter, IServerFilter
{
    public const string TenantParameterKey = "TenantId";

    private readonly ITenantService _tenantService;

    public TenantJobFilter(ITenantService tenantService)
    {
        _tenantService = tenantService;
    }

    // ── IClientFilter ──────────────────────────────────────────────────────

    public void OnCreating(CreatingContext context)
    {
        if (_tenantService.IsMultiTenancyEnabled && !string.IsNullOrEmpty(_tenantService.TenantId))
        {
            context.SetJobParameter(TenantParameterKey, _tenantService.TenantId);
        }
    }

    public void OnCreated(CreatedContext context) { }

    // ── IServerFilter ──────────────────────────────────────────────────────

    public void OnPerforming(PerformingContext context)
    {
        var tenantId = context.GetJobParameter<string>(TenantParameterKey);
        if (!string.IsNullOrEmpty(tenantId))
        {
            // Store the tenant ID in the context items so downstream code can read it
            context.Items[TenantParameterKey] = tenantId;
        }
    }

    public void OnPerformed(PerformedContext context) { }
}
