namespace Monivus.HealthChecks.SqlServer
{
    public class SqlServerHealthCheckOptions
    {
        public string ConnectionString { get; set; }
        public string TestQuery { get; set; }
        public TimeSpan? Timeout { get; set; }
    }
}
