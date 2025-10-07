using System.Data.SqlClient;
using System.Diagnostics;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Monivus.HealthChecks.SqlServer
{
    public class SqlServerHealthCheck : IHealthCheck
    {
        private readonly SqlServerHealthCheckOptions _options;

        public SqlServerHealthCheck(SqlServerHealthCheckOptions options)
        {
            _options = options;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using var connection = new SqlConnection(_options.ConnectionString);
                var openWatch = Stopwatch.StartNew();
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
                openWatch.Stop();

                using var command = new SqlCommand(_options.TestQuery, connection);

                if (_options.Timeout.HasValue)
                {
                    command.CommandTimeout = (int)_options.Timeout.Value.TotalSeconds;
                }

                var queryWatch = Stopwatch.StartNew();
                var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
                queryWatch.Stop();

                if (result != null && result != DBNull.Value)
                {
                    var data = new Dictionary<string, object>
                    {
                        { "DataSource", connection.DataSource },
                        { "Database", connection.Database },
                        { "ConnectionTimeout", connection.ConnectionTimeout },
                        { "State", connection.State },
                        { "ServerVersion", connection.ServerVersion },
                        { "WorkstationId", connection.WorkstationId },
                        { "ClientConnectionId", connection.ClientConnectionId.ToString() },
                        { "CommandTimeoutSeconds", command.CommandTimeout },
                        { "ConnectionOpenMilliseconds", System.Math.Round(openWatch.Elapsed.TotalMilliseconds, 2) },
                        { "QueryDurationMilliseconds", System.Math.Round(queryWatch.Elapsed.TotalMilliseconds, 2) },
                        { "TestQueryResult", result?.ToString() ?? string.Empty }
                    };

                    return HealthCheckResult.Healthy(null, data);
                }

                return HealthCheckResult.Unhealthy(
                    "SQL Server test query returned an unexpected result.",
                    null,
                    new Dictionary<string, object>
                    {
                        { "ConnectionOpenMilliseconds", System.Math.Round(openWatch.Elapsed.TotalMilliseconds, 2) },
                        { "QueryDurationMilliseconds", System.Math.Round(queryWatch.Elapsed.TotalMilliseconds, 2) },
                        { "TestQueryResult", result?.ToString() ?? string.Empty }
                    });
            }
            catch (SqlException ex)
            {
                return HealthCheckResult.Unhealthy(
                    "SQL Server connection failure.",
                    ex,
                    new Dictionary<string, object>
                    {
                        { "ErrorNumber", ex.Number },
                        { "ErrorMessage", ex.Message },
                        { "Server", ex.Server },
                        { "Procedure", ex.Procedure ?? string.Empty },
                        { "LineNumber", ex.LineNumber },
                        { "ClientConnectionId", ex.ClientConnectionId.ToString() }
                    });
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy(
                    "SQL Server access failure.",
                    ex);
            }
        }
    }
}
