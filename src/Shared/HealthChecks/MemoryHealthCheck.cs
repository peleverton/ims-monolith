using System.Diagnostics;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace IMS.Modular.Shared.HealthChecks;

/// <summary>
/// Health check that monitors GC memory pressure.
/// Reports Degraded when allocated bytes exceed a configurable threshold.
/// </summary>
public sealed class MemoryHealthCheck : IHealthCheck
{
    private readonly IOptions<MemoryHealthCheckOptions> _options;

    public MemoryHealthCheck(IOptions<MemoryHealthCheckOptions> options)
    {
        _options = options;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var options = _options.Value;
        var allocatedBytes = GC.GetTotalMemory(forceFullCollection: false);
        var allocatedMb = allocatedBytes / (1024.0 * 1024.0);
        var thresholdMb = options.ThresholdMegabytes;

        var data = new Dictionary<string, object>
        {
            ["AllocatedMegabytes"] = Math.Round(allocatedMb, 2),
            ["ThresholdMegabytes"] = thresholdMb,
            ["Gen0Collections"] = GC.CollectionCount(0),
            ["Gen1Collections"] = GC.CollectionCount(1),
            ["Gen2Collections"] = GC.CollectionCount(2)
        };

        if (allocatedMb >= thresholdMb)
        {
            return Task.FromResult(HealthCheckResult.Degraded(
                description: $"Memory usage is {allocatedMb:F2} MB (threshold: {thresholdMb} MB)",
                data: data));
        }

        return Task.FromResult(HealthCheckResult.Healthy(
            description: $"Memory usage is {allocatedMb:F2} MB (threshold: {thresholdMb} MB)",
            data: data));
    }
}

/// <summary>
/// Configuration options for the memory health check.
/// </summary>
public sealed class MemoryHealthCheckOptions
{
    /// <summary>
    /// Memory threshold in megabytes. Default: 512 MB.
    /// When allocated memory exceeds this, the check reports Degraded.
    /// </summary>
    public double ThresholdMegabytes { get; set; } = 512;
}
