# Monivus.HealthChecks

Reusable building blocks for exposing ASP.NET Core health checks with a Monivus-specific JSON payload, plus optional exporters for shipping results to a central service.

## Health Endpoint

`csharp
app.UseMonivusHealthChecks();
`

## Background Exporter

1. Reference Monivus.HealthChecks.Exporter.
2. Register services:
   `csharp
   builder.Services.AddMonivusExporter(builder.Configuration);
   app.UseMonivusExporter();
   `
3. Configure appsettings:
   `json
   "MonivusExporter": {
     "Enabled": true,
     "TargetApplicationUrl": "https://localhost:5001",
     "CentralAppEndpoint": "https://central.example.com/api/health",
     "ApiKey": "your-secret",
     "CheckIntervalMinutes": 5
   }
   `

Set Enabled to 	rue to start shipping reports. Override CheckIntervalSeconds or HttpTimeout if you need finer control.

## SQL Server Health Check

- Provider: uses `Microsoft.Data.SqlClient` (recommended) under the hood.
- Connection value: accepts either a full connection string or a connection name.
- Resolution rules:
  - If the value matches a name in `ConnectionStrings:{name}`, that connection string is used.
  - Else, if the value looks like a connection string (contains `=`), it is used as-is.
  - Else, the value is used as-is.

Examples

`csharp
// Using a full connection string
builder.Services.AddHealthChecks()
    .AddSqlServerEntry("Server=localhost;Database=SampleDb;Trusted_Connection=True;Encrypt=False;");

// Using a connection name (recommended for environments)
// appsettings.json:
// {
//   "ConnectionStrings": {
//     "sampleDb": "Server=localhost;Database=SampleDb;Trusted_Connection=True;Encrypt=False;"
//   }
// }
builder.Services.AddHealthChecks()
    .AddSqlServerEntry("sampleDb");

// Optional parameters
builder.Services.AddHealthChecks()
    .AddSqlServerEntry(
        connectionStringOrName: "sampleDb",
        testQuery: "SELECT 1",
        name: "sqlserver",
        failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy,
        tags: new [] { "db", "sql" },
        timeout: TimeSpan.FromSeconds(5));
`

Notes

- The overload that takes a `Func<IServiceProvider, string>` remains available if you need dynamic resolution.
- No changes are required to your code if you previously passed a full connection string; connection-name support is additive.

## URL Health Check

Examples

`csharp
// Basic GET expecting 2xx
builder.Services.AddHealthChecks()
    .AddUrlEntry("https://example.com/health");

// Customizing method, expected status codes, and request timeout
builder.Services.AddHealthChecks()
    .AddUrlEntry(
        url: "https://example.com/ping",
        configure: o =>
        {
            o.Method = HttpMethod.Head;
            o.ExpectedStatusCodes = new HashSet<int> { 200, 204 };
            o.RequestTimeout = TimeSpan.FromSeconds(3);
        },
        name: "url:example");

// Dynamic URL from DI/config
builder.Services.AddHealthChecks()
    .AddUrlEntry(
        sp => sp.GetRequiredService<IConfiguration>["Endpoints:Ping"]!,
        name: "url:dynamic");
`

Configuration (UrlHealthCheckOptions defaults)

`json
// appsettings.json (binds to UrlHealthCheckOptions)
{
  "UrlHealthCheck": {
    // TimeSpan format (hh:mm:ss.fff)
    "RequestTimeout": "00:00:02.000",
    // If a response takes longer than this, report Degraded
    "SlowResponseThreshold": "00:00:01.500",
    // Override expected status codes (defaults to 2xx)
    "ExpectedStatusCodes": [200, 204]
  }
}
`

`csharp
// Program.cs
builder.Services.ConfigureUrlHealthChecks(
    builder.Configuration.GetSection("UrlHealthCheck"));

builder.Services.AddHealthChecks()
    .AddUrlEntry("https://example.com/health");
`

Notes

- Defaults are bound to UrlHealthCheckOptions from configuration; not extension parameters.
- Set RequestTimeout and SlowResponseThreshold from configuration; customize Method via the optional configure action.
- If the HTTP call exceeds RequestTimeout, the check returns Unhealthy.
- If the call completes but exceeds SlowResponseThreshold, the check returns Degraded.
