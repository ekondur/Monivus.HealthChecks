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
