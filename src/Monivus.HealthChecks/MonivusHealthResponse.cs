namespace Monivus.HealthChecks
{
    public class HealthCheckReport
    {
        public string Status { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public TimeSpan Duration { get; set; }
        public double DurationMs => Math.Round(Duration.TotalMilliseconds, 2);
        public string TraceId { get; set; } = string.Empty;
        public Dictionary<string, HealthCheckEntry> Entries { get; set; } = [];
        public HealthCheckSummary Summary { get; set; } = new();
    }

    public class HealthCheckEntry
    {
        public string Status { get; set; } = string.Empty;
        public string? Description { get; set; }
        public TimeSpan Duration { get; set; }
        public double DurationMs => Math.Round(Duration.TotalMilliseconds, 2);
        public Dictionary<string, object>? Data { get; set; }
        public string? Exception { get; set; }
        public IEnumerable<string> Tags { get; set; } = [];
    }

    public class HealthCheckSummary
    {
        public int TotalChecks { get; set; }
        public int Healthy { get; set; }
        public int Degraded { get; set; }
        public int Unhealthy { get; set; }
        public int Unknown { get; set; }
        public double TotalDurationMilliseconds { get; set; }
        public double AverageDurationMilliseconds { get; set; }
        public double MaxDurationMilliseconds { get; set; }
    }
}
