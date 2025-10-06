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
