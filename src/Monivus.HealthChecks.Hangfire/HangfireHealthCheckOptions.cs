namespace Monivus.HealthChecks.Hangfire
{
    public class HangfireHealthCheckOptions
    {
        public TimeSpan HeartbeatWindow { get; set; } = TimeSpan.FromMinutes(5);

        public int MinActiveServers { get; set; } = 1;

        public bool DegradeOnFailedJobs { get; set; } = true;

        public bool IncludeDefaultQueueDepth { get; set; } = true;
    }
}
