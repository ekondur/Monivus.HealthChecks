using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Monivus.HealthChecks
{
    public sealed class ResourceUtilizationHealthCheck : IHealthCheck
    {
        private static readonly object CpuSampleLock = new();
        private static double _lastCpuTotalMilliseconds;
        private static DateTime _lastCpuSampleTime = DateTime.MinValue;

        private readonly ResourceUtilizationHealthCheckOptions _options;

        public ResourceUtilizationHealthCheck(ResourceUtilizationHealthCheckOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            var process = Process.GetCurrentProcess();
            var sampleTimeUtc = DateTime.UtcNow;
            var uptime = sampleTimeUtc - process.StartTime.ToUniversalTime();
            if (uptime < TimeSpan.Zero)
            {
                uptime = TimeSpan.Zero;
            }

            var gcInfo = GC.GetGCMemoryInfo();
            var totalAllocatedBytes = GC.GetTotalAllocatedBytes();
            var cpuUsagePercent = CalculateCpuUsagePercent(process, sampleTimeUtc);
            var normalizedCpuUsage = System.Math.Round(System.Math.Clamp(cpuUsagePercent, 0, 100), 2);
            var memoryUsagePercent = CalculateMemoryUsagePercent(gcInfo);

            var metrics = new Dictionary<string, object>
            {
                ["ProcessId"] = process.Id,
                ["ProcessName"] = process.ProcessName,
                ["MachineName"] = Environment.MachineName,
                ["Is64BitProcess"] = Environment.Is64BitProcess,
                ["ProcessorCount"] = Environment.ProcessorCount,
                ["OsDescription"] = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
                ["ProcessArchitecture"] = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString(),
                ["FrameworkDescription"] = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
                ["UptimeSeconds"] = System.Math.Round(uptime.TotalSeconds, 2),
                ["TotalProcessorTimeSeconds"] = System.Math.Round(process.TotalProcessorTime.TotalSeconds, 2),
                ["CpuUsagePercent"] = normalizedCpuUsage,
                ["WorkingSetBytes"] = process.WorkingSet64,
                ["WorkingSetMegabytes"] = System.Math.Round(process.WorkingSet64 / 1024d / 1024d, 2),
                ["PrivateMemoryBytes"] = process.PrivateMemorySize64,
                ["PagedMemoryBytes"] = process.PagedMemorySize64,
                ["PagedSystemMemoryBytes"] = process.PagedSystemMemorySize64,
                ["NonPagedSystemMemoryBytes"] = process.NonpagedSystemMemorySize64,
                ["EnvironmentWorkingSetBytes"] = Environment.WorkingSet,
                ["HandleCount"] = process.HandleCount,
                ["ThreadCount"] = process.Threads.Count,
                ["PriorityClass"] = process.PriorityClass.ToString(),
                ["GcTotalAllocatedBytes"] = totalAllocatedBytes,
                ["GcHeapSizeBytes"] = gcInfo.HeapSizeBytes,
                ["GcFragmentedBytes"] = gcInfo.FragmentedBytes,
                ["GcCommittedBytes"] = gcInfo.TotalCommittedBytes,
                ["GcMemoryLoadBytes"] = gcInfo.MemoryLoadBytes,
                ["GcHighMemoryLoadThresholdBytes"] = gcInfo.HighMemoryLoadThresholdBytes,
                ["GcTotalAvailableMemoryBytes"] = gcInfo.TotalAvailableMemoryBytes,
                ["GcCollectionsGen0"] = GC.CollectionCount(0),
                ["GcCollectionsGen1"] = GC.CollectionCount(1),
                ["GcCollectionsGen2"] = GC.CollectionCount(2),
                ["GcPinnedObjectsCount"] = gcInfo.PinnedObjectsCount,
                ["TimestampUtc"] = sampleTimeUtc.ToString("o")
            };

            if (memoryUsagePercent.HasValue)
            {
                metrics["MemoryUsagePercent"] = System.Math.Round(System.Math.Clamp(memoryUsagePercent.Value, 0, 100), 2);
            }

            if (gcInfo.Generation >= 0)
            {
                metrics["GcGeneration"] = gcInfo.Generation;
            }

            if (gcInfo.PauseTimePercentage >= 0)
            {
                metrics["GcPauseTimePercentage"] = System.Math.Round(gcInfo.PauseTimePercentage, 2);
            }

            var status = HealthStatus.Healthy;
            var description = "Resource utilization within defined thresholds.";

            if (memoryUsagePercent.HasValue &&
                memoryUsagePercent.Value >= _options.MemoryUsageDegradedThresholdPercent)
            {
                status = HealthStatus.Degraded;
                description = $"Process memory usage {System.Math.Round(memoryUsagePercent.Value, 2)}% exceeds {_options.MemoryUsageDegradedThresholdPercent}% threshold.";
            }
            else if (normalizedCpuUsage >= _options.CpuUsageDegradedThresholdPercent)
            {
                status = HealthStatus.Degraded;
                description = $"Process CPU usage {normalizedCpuUsage}% exceeds {_options.CpuUsageDegradedThresholdPercent}% threshold.";
            }

            return Task.FromResult(new HealthCheckResult(
                status,
                description,
                null,
                metrics));
        }

        private static double CalculateCpuUsagePercent(Process process, DateTime sampleTimeUtc)
        {
            lock (CpuSampleLock)
            {
                var totalCpu = process.TotalProcessorTime.TotalMilliseconds;

                if (_lastCpuSampleTime == DateTime.MinValue)
                {
                    _lastCpuSampleTime = sampleTimeUtc;
                    _lastCpuTotalMilliseconds = totalCpu;
                    var uptime = sampleTimeUtc - process.StartTime.ToUniversalTime();
                    var baselineMs = System.Math.Max(uptime.TotalMilliseconds, 1);
                    return totalCpu / (Environment.ProcessorCount * baselineMs) * 100;
                }

                var elapsed = (sampleTimeUtc - _lastCpuSampleTime).TotalMilliseconds;
                var cpuDelta = totalCpu - _lastCpuTotalMilliseconds;

                _lastCpuSampleTime = sampleTimeUtc;
                _lastCpuTotalMilliseconds = totalCpu;

                if (elapsed <= 0)
                {
                    return 0;
                }

                return cpuDelta / (Environment.ProcessorCount * elapsed) * 100;
            }
        }

        private static double? CalculateMemoryUsagePercent(GCMemoryInfo gcInfo)
        {
            return gcInfo.TotalAvailableMemoryBytes > 0
                ? gcInfo.MemoryLoadBytes / (double)gcInfo.TotalAvailableMemoryBytes * 100
                : null;
        }
    }

    public class ResourceUtilizationHealthCheckOptions
    {
        public double MemoryUsageDegradedThresholdPercent { get; set; } = 90;
        public double CpuUsageDegradedThresholdPercent { get; set; } = 90;
    }
}
