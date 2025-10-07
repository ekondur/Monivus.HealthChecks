using System;
using System.Collections.Generic;
using System.Linq;
using Hangfire.Storage;
using Hangfire.Storage.Monitoring;
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

                var now = DateTime.UtcNow;
                var heartbeatWindow = TimeSpan.FromMinutes(5);
                var activeServers = servers.Count(s => s.Heartbeat.HasValue &&
                    (now - s.Heartbeat.Value).TotalMinutes < heartbeatWindow.TotalMinutes);

                if (activeServers == 0)
                {
                    return Task.FromResult(HealthCheckResult.Unhealthy("No active Hangfire servers detected"));
                }

                var stats = _monitoringApi.GetStatistics();
                if (stats == null)
                {
                    return Task.FromResult(HealthCheckResult.Unhealthy("Hangfire storage connection failed"));
                }

                var queues = _monitoringApi.Queues() ?? new List<QueueWithTopEnqueuedJobsDto>();
                var queueSummaries = queues.ToDictionary(
                    q => q.Name,
                    q => (object)new Dictionary<string, object>
                    {
                        ["Length"] = q.Length,
                        ["Fetched"] = q.Fetched
                    });

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
                    ["LastServerHeartbeatUtc"] = lastHeartbeat == DateTime.MinValue ? null : lastHeartbeat.ToString("o"),
                    ["SucceededJobs"] = stats.Succeeded,
                    ["FailedJobs"] = stats.Failed,
                    ["ProcessingJobs"] = stats.Processing,
                    ["ScheduledJobs"] = stats.Scheduled,
                    ["TotalEnqueuedJobs"] = stats.Enqueued,
                    ["DefaultQueueDepth"] = _monitoringApi.EnqueuedCount("default"),
                    ["DeletedJobs"] = stats.Deleted,
                    ["Retries"] = stats.Retries ?? 0,
                    ["RecurringJobs"] = stats.Recurring,
                    ["QueuesCount"] = queueSummaries.Count,
                    ["Queues"] = queueSummaries
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
