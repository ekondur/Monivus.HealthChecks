using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using System.Reflection;
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
            var response = new MonivusHealthResponse
            {
                Status = report.Status.ToString(),
                Timestamp = DateTime.UtcNow,
                Duration = report.TotalDuration,
                Application = new MonivusApplicationInfo
                {
                    Name = Assembly.GetEntryAssembly()?.GetName().Name,
                    Version = Assembly.GetEntryAssembly()?.GetName().Version?.ToString(),
                    Environment = context.RequestServices.GetService<IHostEnvironment>()?.EnvironmentName,
                    MachineName = Environment.MachineName
                },
                TraceId = context.TraceIdentifier,
                Checks = report.Entries.ToDictionary(
                    e => e.Key,
                    e => new MonivusHealthCheckDetail
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
