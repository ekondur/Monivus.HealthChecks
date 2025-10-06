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

        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var servers = _monitoringApi.Servers();
                if (servers == null)
                {
                    return Task.FromResult(HealthCheckResult.Unhealthy("Hangfire storage is not accessible"));
                }

                var failedJobs = _monitoringApi.FailedCount();
                if (failedJobs > 0)
                {
                    return Task.FromResult(HealthCheckResult.Degraded(
                        $"Hangfire is running but has {failedJobs} failed job(s)",
                        data: new Dictionary<string, object> { ["FailedJobs"] = failedJobs }));
                }

                var activeServers = servers.Count(s => s.Heartbeat.HasValue &&
                    (DateTime.UtcNow - s.Heartbeat.Value).TotalMinutes < 5);

                if (activeServers == 0)
                {
                    return Task.FromResult(HealthCheckResult.Unhealthy("No active Hangfire servers detected"));
                }

                var stats = _monitoringApi.GetStatistics();
                if (stats == null)
                {
                    return Task.FromResult(HealthCheckResult.Unhealthy("Hangfire storage connection failed"));
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

                return Task.FromResult(HealthCheckResult.Healthy(
                    "Hangfire is healthy and running",
                    healthData));
            }
            catch (Exception ex)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy(
                    "Hangfire health check failed",
                    ex,
                    new Dictionary<string, object> { ["Error"] = ex.Message }));
            }
        }
    }
}
