using Hangfire;
using Microsoft.EntityFrameworkCore;
using Monivus.ApiTest;
using Monivus.HealthChecks;
using Monivus.HealthChecks.Exporter;
using Monivus.HealthChecks.Hangfire;
using Monivus.HealthChecks.Redis;
using Monivus.HealthChecks.SqlServer;
using StackExchange.Redis;
using System.Configuration;

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.

        builder.Services.AddControllers();
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        builder.Services.AddDbContext<SampleDbContext>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

        builder.Services.AddHangfire(configuration => configuration
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection")));

        builder.Services.AddHangfireServer();

        // Register Redis distributed cache
        builder.Services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = builder.Configuration["Redis"];
            options.InstanceName = builder.Configuration["Redis:InstanceName"];
        });

        // Register Redis connection
        builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var configuration = ConfigurationOptions.Parse(builder.Configuration["Redis:ConnectionString"], true);
            return ConnectionMultiplexer.Connect(configuration);
        });

        builder.Services.AddHealthChecks()
            .AddDbContextCheck<SampleDbContext>()
            .AddResourceUtilizationHealthCheck()
            .AddSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
            .AddHangfire()
            .AddRedis();

        builder.Services.AddMonivusExporter(builder.Configuration);

        var app = builder.Build();

        app.UseMonivusHealthChecks();
        app.UseMonivusExporter();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        app.UseAuthorization();

        app.UseHangfireDashboard();

        app.MapControllers();

        app.Run();
    }
}
