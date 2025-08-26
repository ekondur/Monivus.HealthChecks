namespace Monivus.HealthChecks
{
    public class MonivusHealthResponse
    {
        public string Status { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public TimeSpan Duration { get; set; }
        public string TraceId { get; set; } = string.Empty;
        public Dictionary<string, MonivusHealthCheckDetail> Checks { get; set; } = [];
        public MonivusApplicationInfo Application { get; set; } = new();
    }

    public class MonivusHealthCheckDetail
    {
        public string Status { get; set; } = string.Empty;
        public string? Description { get; set; }
        public TimeSpan Duration { get; set; }
        public Dictionary<string, object>? Data { get; set; }
        public string? Exception { get; set; }
        public IEnumerable<string> Tags { get; set; } = [];
    }

    public class MonivusApplicationInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Environment { get; set; } = string.Empty;
        public string MachineName { get; set; } = string.Empty;
    }
}
