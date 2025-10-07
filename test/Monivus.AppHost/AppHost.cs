var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<Projects.Monivus_Api>("api");

builder.AddProject<Projects.Monivus_UI>("ui")
    .WithExternalHttpEndpoints()
    .WithReference(api)
    .WaitFor(api);

builder.Build().Run();
