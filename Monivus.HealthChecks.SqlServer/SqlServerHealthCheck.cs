using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Data.SqlClient;

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
                await connection.OpenAsync(cancellationToken);

                // Basit bir test query çalıştır
                using var command = new SqlCommand(_options.TestQuery, connection);
                var result = await command.ExecuteScalarAsync(cancellationToken);

                if (result != null && result != DBNull.Value)
                {
                    return HealthCheckResult.Healthy(null,
                    new Dictionary<string, object>
                    {
                        { "server", connection.DataSource },
                        { "database", connection.Database },
                        { "connection_timeout", connection.ConnectionTimeout },
                        { "state", connection.State },
                        { "server_version", connection.ServerVersion },
                        { "workstation_id", connection.WorkstationId }
                        });
                }
                else
                {
                    return HealthCheckResult.Unhealthy(
                        "SQL Server test sorgusu beklenen sonucu döndürmedi");
                }
            }
            catch (SqlException ex)
            {
                return HealthCheckResult.Unhealthy(
                    "SQL Server bağlantı hatası",
                    ex,
                    new Dictionary<string, object>
                    {
                    { "error_number", ex.Number },
                    { "error_message", ex.Message },
                    { "server", ex.Server }
                    });
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy(
                    "SQL Server erişim hatası",
                    ex);
            }
        }
    }
}
