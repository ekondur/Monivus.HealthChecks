using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text.Json;

namespace Monivus.HealthChecks
{
    public static class MonivusHealthCheckExtensions
    {
        private static readonly JsonSerializerOptions CachedJsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        public static IApplicationBuilder UseMonivusHealthChecks(this IApplicationBuilder app, string path = "/health")
        {
            return app.UseHealthChecks(path, new HealthCheckOptions
            {
                ResponseWriter = WriteMonivusHealthResponse
            });
        }

        private static Task WriteMonivusHealthResponse(HttpContext context, HealthReport report)
        {
            var entryResults = new Dictionary<string, HealthCheckEntry>(report.Entries.Count, System.StringComparer.OrdinalIgnoreCase);
            var totalEntryDurationMs = 0d;
            var maxEntryDurationMs = 0d;
            var healthyCount = 0;
            var degradedCount = 0;
            var unhealthyCount = 0;
            var unknownCount = 0;

            foreach (var entry in report.Entries)
            {
                var source = entry.Value;
                var responseEntry = new HealthCheckEntry
                {
                    Status = source.Status.ToString(),
                    Description = source.Description,
                    Duration = source.Duration,
                    Data = source.Data?.ToDictionary(
                        d => d.Key,
                        d => d.Value is Exception ex ? ex.Message : d.Value),
                    Exception = source.Exception?.GetType().FullName,
                    Tags = source.Tags
                };

                entryResults[entry.Key] = responseEntry;

                var durationMs = responseEntry.Duration.TotalMilliseconds;
                totalEntryDurationMs += durationMs;
                if (durationMs > maxEntryDurationMs)
                {
                    maxEntryDurationMs = durationMs;
                }

                switch (source.Status)
                {
                    case HealthStatus.Healthy:
                        healthyCount++;
                        break;
                    case HealthStatus.Degraded:
                        degradedCount++;
                        break;
                    case HealthStatus.Unhealthy:
                        unhealthyCount++;
                        break;
                    default:
                        unknownCount++;
                        break;
                }
            }

            var response = new HealthCheckReport
            {
                Status = report.Status.ToString(),
                Timestamp = DateTime.UtcNow,
                Duration = report.TotalDuration,
                TraceId = context.TraceIdentifier,
                Entries = entryResults
            };

            context.Response.ContentType = "application/json";

            return context.Response.WriteAsync(
                JsonSerializer.Serialize(response, CachedJsonOptions));
        }
    }
}
