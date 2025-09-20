namespace Monivus.HealthChecks
{
    public class HealthCheckReport
    {
        public string Status { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public TimeSpan Duration { get; set; }
        public string TraceId { get; set; } = string.Empty;
        public Dictionary<string, HealthCheckEntry> Entries { get; set; } = [];
    }

    public class HealthCheckEntry
    {
        public string Status { get; set; } = string.Empty;
        public string? Description { get; set; }
        public TimeSpan Duration { get; set; }
        public Dictionary<string, object>? Data { get; set; }
        public string? Exception { get; set; }
        public IEnumerable<string> Tags { get; set; } = [];
    }
}
