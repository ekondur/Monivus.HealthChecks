using Hangfire.Storage;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Monivus.HealthChecks.Hangfire
{
    public class HangfireHealthCheck : IHealthCheck
    {
        private readonly IMonitoringApi _monitoringApi;

        public HangfireHealthCheck(IMonitoringApi monitoringApi)
        {
            _monitoringApi = monitoringApi;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Check if Hangfire storage is accessible
                var servers = _monitoringApi.Servers();
                if (servers == null)
                {
                    return HealthCheckResult.Unhealthy("Hangfire storage is not accessible");
                }

                // Check for failed jobs
                var failedJobs = _monitoringApi.FailedCount();
                if (failedJobs > 0)
                {
                    return HealthCheckResult.Degraded(
                        $"Hangfire is running but has {failedJobs} failed job(s)",
                        data: new Dictionary<string, object> { ["FailedJobs"] = failedJobs });
                }

                // Check if any servers are connected
                var activeServers = servers.Count(s => s.Heartbeat.HasValue &&
                    (DateTime.UtcNow - s.Heartbeat.Value).TotalMinutes < 5);

                if (activeServers == 0)
                {
                    return HealthCheckResult.Unhealthy("No active Hangfire servers detected");
                }

                // Check storage connectivity
                var stats = _monitoringApi.GetStatistics();
                if (stats == null)
                {
                    return HealthCheckResult.Unhealthy("Hangfire storage connection failed");
                }

                var healthData = new Dictionary<string, object>
                {
                    ["ActiveServers"] = activeServers,
                    ["TotalServers"] = servers.Count,
                    ["SucceededJobs"] = stats.Succeeded,
                    ["FailedJobs"] = stats.Failed,
                    ["ProcessingJobs"] = stats.Processing,
                    ["ScheduledJobs"] = stats.Scheduled,
                    ["EnqueuedJobs"] = _monitoringApi.EnqueuedCount("default"),
                    ["DeletedJobs"] = stats.Deleted,
                    ["Retries"] = stats.Retries ?? 0
                };

                return HealthCheckResult.Healthy(
                    "Hangfire is healthy and running",
                    healthData);
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy(
                    "Hangfire health check failed",
                    ex,
                    new Dictionary<string, object> { ["Error"] = ex.Message });
            }
        }
    }
}
