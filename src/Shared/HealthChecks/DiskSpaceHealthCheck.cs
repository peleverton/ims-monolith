using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace IMS.Modular.Shared.HealthChecks;

/// <summary>
/// Health check that monitors available disk space.
/// Reports Degraded when free space drops below a configurable threshold.
/// </summary>
public sealed class DiskSpaceHealthCheck : IHealthCheck
{
    private readonly IOptions<DiskSpaceHealthCheckOptions> _options;

    public DiskSpaceHealthCheck(IOptions<DiskSpaceHealthCheckOptions> options)
    {
        _options = options;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var options = _options.Value;
        var drivePath = options.DrivePath;

        try
        {
            var driveInfo = new DriveInfo(drivePath);

            if (!driveInfo.IsReady)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy(
                    description: $"Drive '{drivePath}' is not ready"));
            }

            var freeSpaceMb = driveInfo.AvailableFreeSpace / (1024.0 * 1024.0);
            var totalSpaceMb = driveInfo.TotalSize / (1024.0 * 1024.0);
            var usedPercentage = ((totalSpaceMb - freeSpaceMb) / totalSpaceMb) * 100;
            var thresholdMb = options.MinimumFreeMegabytes;

            var data = new Dictionary<string, object>
            {
                ["Drive"] = drivePath,
                ["TotalSpaceMB"] = Math.Round(totalSpaceMb, 2),
                ["FreeSpaceMB"] = Math.Round(freeSpaceMb, 2),
                ["UsedPercentage"] = Math.Round(usedPercentage, 2),
                ["ThresholdMB"] = thresholdMb
            };

            if (freeSpaceMb < thresholdMb)
            {
                return Task.FromResult(HealthCheckResult.Degraded(
                    description: $"Low disk space: {freeSpaceMb:F0} MB free (threshold: {thresholdMb} MB)",
                    data: data));
            }

            return Task.FromResult(HealthCheckResult.Healthy(
                description: $"Disk space OK: {freeSpaceMb:F0} MB free ({usedPercentage:F1}% used)",
                data: data));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                description: $"Failed to check disk space for '{drivePath}'",
                exception: ex));
        }
    }
}

/// <summary>
/// Configuration options for the disk space health check.
/// </summary>
public sealed class DiskSpaceHealthCheckOptions
{
    /// <summary>
    /// Drive/mount path to check. Default: "/" (root on Linux/macOS) or "C:\\" on Windows.
    /// </summary>
    public string DrivePath { get; set; } = OperatingSystem.IsWindows() ? "C:\\" : "/";

    /// <summary>
    /// Minimum free space in megabytes. Default: 1024 MB (1 GB).
    /// When free space drops below this, the check reports Degraded.
    /// </summary>
    public double MinimumFreeMegabytes { get; set; } = 1024;
}
