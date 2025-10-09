namespace Monivus.HealthChecks
{
    public class AggregatedHealthOptions
    {
        // Backward-compat single remote endpoint
        public string RemoteHealthEndpoint { get; set; } = string.Empty;
        public string RemoteEntryPrefix { get; set; } = "api";

        // Multiple remote endpoints
        public IList<RemoteHealthEndpoint> RemoteEndpoints { get; set; } = new List<RemoteHealthEndpoint>();

        // Default timeout for remote HTTP calls
        public TimeSpan HttpTimeout { get; set; } = TimeSpan.FromSeconds(5);

        // Include a summary entry for each remote call
        public bool IncludeRemoteSummaryEntry { get; set; } = true;

        // Normalize options to support single-endpoint configuration
        internal void Normalize()
        {
            if (RemoteEndpoints.Count == 0 && !string.IsNullOrWhiteSpace(RemoteHealthEndpoint))
            {
                RemoteEndpoints.Add(new RemoteHealthEndpoint
                {
                    Url = RemoteHealthEndpoint,
                    Prefix = RemoteEntryPrefix
                });
            }
        }

        // Fluent helpers
        public AggregatedHealthOptions AddEndpoint(string prefix, string url, TimeSpan? timeout = null)
        {
            if (string.IsNullOrWhiteSpace(prefix)) throw new ArgumentException("Prefix must be provided", nameof(prefix));
            if (string.IsNullOrWhiteSpace(url)) throw new ArgumentException("Url must be provided", nameof(url));
            if (!Uri.TryCreate(url, UriKind.Absolute, out var parsed) ||
                (parsed.Scheme != Uri.UriSchemeHttp && parsed.Scheme != Uri.UriSchemeHttps))
            {
                throw new ArgumentException("Url must be an absolute HTTP/HTTPS URL", nameof(url));
            }

            RemoteEndpoints.Add(new RemoteHealthEndpoint
            {
                Url = url,
                Prefix = prefix,
                HttpTimeout = timeout
            });
            return this;
        }

        public AggregatedHealthOptions AddEndpoint(string url)
            => AddEndpoint("remote", url, null);
    }

    public class RemoteHealthEndpoint
    {
        public string Url { get; set; } = string.Empty; // absolute URL
        public string Prefix { get; set; } = "api";     // entry prefix
        public TimeSpan? HttpTimeout { get; set; }       // optional per-endpoint timeout
    }
}
