using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Monivus.HealthChecks.SqlServer
{
    public static class SqlServerHealthCheckBuilderExtensions
    {
        public static IHealthChecksBuilder AddSqlServer(
            this IHealthChecksBuilder builder,
            string connectionString,
            string testQuery = "SELECT 1",
            string name = null,
            HealthStatus? failureStatus = null,
            IEnumerable<string> tags = null,
            TimeSpan? timeout = null)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));
            if (string.IsNullOrEmpty(connectionString))
                throw new ArgumentNullException(nameof(connectionString));

            var options = new SqlServerHealthCheckOptions
            {
                ConnectionString = connectionString,
                TestQuery = testQuery,
                Timeout = timeout
            };

            return builder.Add(new HealthCheckRegistration(
                name ?? "sqlserver",
                sp => new SqlServerHealthCheck(options),
                failureStatus,
                tags,
                timeout));
        }

        public static IHealthChecksBuilder AddSqlServer(
            this IHealthChecksBuilder builder,
            Func<IServiceProvider, string> connectionStringFactory,
            string testQuery = "SELECT 1",
            string name = null,
            HealthStatus? failureStatus = null,
            IEnumerable<string> tags = null,
            TimeSpan? timeout = null)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));
            if (connectionStringFactory == null)
                throw new ArgumentNullException(nameof(connectionStringFactory));

            return builder.Add(new HealthCheckRegistration(
                name ?? "sqlserver",
                sp =>
                {
                    var connectionString = connectionStringFactory(sp);
                    var options = new SqlServerHealthCheckOptions
                    {
                        ConnectionString = connectionString,
                        TestQuery = testQuery
                    };

                    if (timeout.HasValue)
                    {
                        options.Timeout = timeout.Value;
                    }

                    return new SqlServerHealthCheck(options);
                },
                failureStatus,
                tags,
                timeout));
        }
    }
}
