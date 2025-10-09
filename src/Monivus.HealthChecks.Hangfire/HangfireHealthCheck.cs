using Hangfire.Storage;
using Hangfire.Storage.Monitoring;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Monivus.HealthChecks.Hangfire
{
    public class HangfireHealthCheck : IHealthCheck
    {
        private readonly IMonitoringApi _monitoringApi;
        private readonly HangfireHealthCheckOptions _options;

        public HangfireHealthCheck(IMonitoringApi monitoringApi, HangfireHealthCheckOptions? options = null)
        {
            _monitoringApi = monitoringApi ?? throw new ArgumentNullException(nameof(monitoringApi));
            _options = options ?? new HangfireHealthCheckOptions();
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

                var stats = _monitoringApi.GetStatistics();
                if (stats == null)
                {
                    return Task.FromResult(HealthCheckResult.Unhealthy("Hangfire storage connection failed"));
                }

                var failedJobs = stats.Failed;
                if (failedJobs > 0 && _options.DegradeOnFailedJobs)
                {
                    return Task.FromResult(HealthCheckResult.Degraded(
                        $"Hangfire is running but has {failedJobs} failed job(s)",
                        data: new Dictionary<string, object> { ["FailedJobs"] = failedJobs }));
                }

                var now = DateTime.UtcNow;
                var heartbeatWindow = _options.HeartbeatWindow;
                var activeServers = servers.Count(s => s.Heartbeat.HasValue &&
                    (now - s.Heartbeat.Value).TotalMinutes < heartbeatWindow.TotalMinutes);

                if (activeServers < _options.MinActiveServers)
                {
                    return Task.FromResult(HealthCheckResult.Unhealthy("Insufficient active Hangfire servers detected"));
                }

                // Queues and per-queue metrics intentionally not collected for performance.

                var lastHeartbeat = servers
                    .Where(s => s.Heartbeat.HasValue)
                    .Select(s => s.Heartbeat!.Value)
                    .DefaultIfEmpty(DateTime.MinValue)
                    .Max();

                var healthData = new Dictionary<string, object>
                {
                    ["ActiveServers"] = activeServers,
                    ["TotalServers"] = servers.Count,
                    ["ServersWithoutRecentHeartbeat"] = servers.Count - activeServers,
                    ["SucceededJobs"] = stats.Succeeded,
                    ["FailedJobs"] = stats.Failed,
                    ["ProcessingJobs"] = stats.Processing,
                    ["ScheduledJobs"] = stats.Scheduled,
                    ["TotalEnqueuedJobs"] = stats.Enqueued,
                    ["DeletedJobs"] = stats.Deleted,
                    ["RecurringJobs"] = stats.Recurring,
                };

                if (lastHeartbeat != DateTime.MinValue)
                {
                    healthData["LastServerHeartbeatUtc"] = lastHeartbeat.ToString("o");
                }

                if (_options.IncludeDefaultQueueDepth)
                {
                    healthData["DefaultQueueDepth"] = _monitoringApi.EnqueuedCount("default");
                }

                // Removed "Queues" and "QueuesCount" metrics per performance requirement.

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
