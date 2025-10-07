using Hangfire;
using Microsoft.EntityFrameworkCore;
using Monivus.ApiTest;
using Monivus.HealthChecks;
using Monivus.HealthChecks.Exporter;
using Monivus.HealthChecks.Hangfire;
using Monivus.HealthChecks.Redis;
using Monivus.HealthChecks.SqlServer;
using StackExchange.Redis;

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var configuration = builder.Configuration;
        var defaultConnection = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is missing.");

        var redisConnectionString = configuration["Redis:ConnectionString"]
            ?? throw new InvalidOperationException("Redis:ConnectionString is missing.");
        var redisInstanceName = configuration["Redis:InstanceName"];


        // Add services to the container.

        builder.Services.AddControllers();
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        builder.Services.AddDbContext<SampleDbContext>(options =>
            options.UseSqlServer(defaultConnection));

        builder.Services.AddHangfire(configuration => configuration
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UseSqlServerStorage(defaultConnection));

        builder.Services.AddHangfireServer();

        // Register Redis distributed cache
        builder.Services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnectionString;
            options.InstanceName = redisInstanceName;
        });

        // Bind Redis health check options from configuration
        builder.Services.AddOptions<RedisHealthCheckOptions>()
            .Bind(configuration.GetSection("Redis"));

        // Register Redis connection
        builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var configuration = ConfigurationOptions.Parse(redisConnectionString, true);
            return ConnectionMultiplexer.Connect(configuration);
        });

        builder.Services.AddHealthChecks()
            .AddResourceUtilization(name: "Resource Utilization")
            .AddSqlServer(defaultConnection, name: "Sql Server 1")
            .AddHangfire()
            .AddRedis(name: "Redis Cluster");

        builder.Services.AddMonivusExporter(configuration);

        var app = builder.Build();

        app.UseMonivusHealthChecks();
        //app.UseMonivusExporter();

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
