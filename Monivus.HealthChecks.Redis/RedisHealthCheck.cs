using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace Monivus.HealthChecks.Redis
{
    public class RedisHealthCheck : IHealthCheck
    {
        private readonly IConnectionMultiplexer _redisConnection;

        public RedisHealthCheck(IConnectionMultiplexer redisConnection)
        {
            _redisConnection = redisConnection ?? throw new ArgumentNullException(nameof(redisConnection));
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Check if connection is established and healthy
                if (!_redisConnection.IsConnected)
                {
                    return HealthCheckResult.Unhealthy(
                        "Redis connection is not established",
                        exception: null,
                        data: new Dictionary<string, object>
                        {
                            ["ConnectionState"] = "Disconnected",
                            ["Timestamp"] = DateTime.UtcNow
                        });
                }

                var database = _redisConnection.GetDatabase();
                var endpoints = _redisConnection.GetEndPoints();
                var server = _redisConnection.GetServer(endpoints.First());

                // Perform a simple PING command to test connectivity
                var pingResponse = await database.PingAsync();

                // Get server information for detailed health status
                var serverInfo = await server.InfoAsync();

                var healthData = new Dictionary<string, object>
                {
                    ["PingTime"] = pingResponse.TotalMilliseconds,
                    ["DatabaseSize"] = await server.DatabaseSizeAsync(),
                    ["LastSave"] = (await server.LastSaveAsync()).ToString("u"),
                    ["ServerVersion"] = server.Version.ToString(),
                    ["ServerType"] = server.ServerType.ToString(),
                    ["IsConnected"] = server.IsConnected,
                };

                // Check if server is responding within reasonable time (optional basic check)
                if (pingResponse.TotalMilliseconds > 1000) // 1 second threshold for demo
                {
                    return HealthCheckResult.Degraded(
                        $"Redis is responding slowly. Ping: {pingResponse.TotalMilliseconds}ms",
                        data: healthData);
                }

                return HealthCheckResult.Healthy(
                    "Redis is healthy and responsive",
                    data: healthData);
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy(
                    "Redis health check failed",
                    exception: ex,
                    data: new Dictionary<string, object>
                    {
                        ["ErrorMessage"] = ex.Message,
                        ["ExceptionType"] = ex.GetType().Name,
                        ["Timestamp"] = DateTime.UtcNow.ToString("u")
                    });
            }
        }
    }
}
