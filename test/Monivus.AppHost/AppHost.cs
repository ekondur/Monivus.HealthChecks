var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<Projects.Monivus_Api>("api");

var cache = builder
    .AddRedis("cache")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

builder.AddProject<Projects.Monivus_UI>("ui")
    .WithExternalHttpEndpoints()
    .WithReference(api)
    .WithReference(cache)
    .WaitFor(api)
    .WaitFor(cache);

builder.Build().Run();
