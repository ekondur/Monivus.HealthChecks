﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace Monivus.HealthChecks.Redis
{
    public static class RedisHealthCheckExtensions
    {
        /// <summary>
        /// Adds a health check for Redis using the provided connection multiplexer
        /// </summary>
        /// <param name="builder">The health checks builder</param>
        /// <param name="name">The health check name (optional)</param>
        /// <param name="failureStatus">The status to report when the health check fails (optional)</param>
        /// <param name="tags">A list of tags that can be used to filter health checks (optional)</param>
        /// <param name="timeout">The health check timeout (optional)</param>
        /// <returns>The health checks builder</returns>
        public static IHealthChecksBuilder AddRedis(
            this IHealthChecksBuilder builder,
            string name = "redis",
            HealthStatus? failureStatus = null,
            IEnumerable<string>? tags = null,
            TimeSpan? timeout = null)
        {
            ArgumentNullException.ThrowIfNull(builder);

            // Register the health check with the provided parameters
            var registration = new HealthCheckRegistration(
                name,
                serviceProvider =>
                {
                    var redisConnection = serviceProvider.GetRequiredService<IConnectionMultiplexer>();
                    return new RedisHealthCheck(redisConnection);
                },
                failureStatus,
                tags,
                timeout);

            return builder.Add(registration);
        }

        /// <summary>
        /// Adds a health check for Redis using a connection string
        /// </summary>
        /// <param name="builder">The health checks builder</param>
        /// <param name="connectionString">The Redis connection string</param>
        /// <param name="name">The health check name (optional)</param>
        /// <param name="failureStatus">The status to report when the health check fails (optional)</param>
        /// <param name="tags">A list of tags that can be used to filter health checks (optional)</param>
        /// <param name="timeout">The health check timeout (optional)</param>
        /// <returns>The health checks builder</returns>
        public static IHealthChecksBuilder AddRedis(
            this IHealthChecksBuilder builder,
            string connectionString,
            string name = "redis",
            HealthStatus? failureStatus = null,
            IEnumerable<string>? tags = null,
            TimeSpan? timeout = null)
        {
            ArgumentNullException.ThrowIfNull(builder);

            if (string.IsNullOrEmpty(connectionString))
                throw new ArgumentException("Connection string cannot be null or empty", nameof(connectionString));

            // Register the health check with its own connection
            var registration = new HealthCheckRegistration(
                name,
                _ =>
                {
                    var redisConnection = ConnectionMultiplexer.Connect(connectionString);
                    return new RedisHealthCheck(redisConnection);
                },
                failureStatus,
                tags,
                timeout);

            return builder.Add(registration);
        }
    }
}
