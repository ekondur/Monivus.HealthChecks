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
            var response = new HealthCheckReport
            {
                Status = report.Status.ToString(),
                Timestamp = DateTime.UtcNow,
                Duration = report.TotalDuration,
                TraceId = context.TraceIdentifier,
                Entries = report.Entries.ToDictionary(
                    e => e.Key,
                    e => new HealthCheckEntry
                    {
                        Status = e.Value.Status.ToString(),
                        Description = e.Value.Description,
                        Duration = e.Value.Duration,
                        Data = e.Value.Data?.ToDictionary(
                            d => d.Key,
                            d => d.Value is Exception ex ? ex.Message : d.Value),
                        Exception = e.Value.Exception?.GetType().FullName,
                        Tags = e.Value.Tags
                    })
            };

            context.Response.ContentType = "application/json";

            return context.Response.WriteAsync(
                JsonSerializer.Serialize(response, CachedJsonOptions));
        }
    }
}
