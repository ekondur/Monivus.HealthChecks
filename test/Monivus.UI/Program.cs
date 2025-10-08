using Monivus.UI.Components;
using Monivus.HealthChecks;
using Monivus.HealthChecks.Exporter;
using Monivus.HealthChecks.Redis;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

// Add services to the container.
builder.AddServiceDefaults();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.AddRedisDistributedCache(connectionName: "cache",
    settings => 
    { 
        settings.DisableHealthChecks = true;
    },
    options =>
    {
        options.AllowAdmin = true;
    });

builder.Services.AddHealthChecks()
    .AddResourceUtilizationEntry()
    .AddRedisEntry();

builder.Services.AddMonivusExporter(configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseMonivusHealthChecks();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
