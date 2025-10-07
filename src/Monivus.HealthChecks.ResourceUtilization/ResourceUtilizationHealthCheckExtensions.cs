using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Monivus.HealthChecks
{
    public static class ResourceUtilizationHealthCheckExtensions
    {
        public static IHealthChecksBuilder AddResourceUtilization(
            this IHealthChecksBuilder builder,
            Action<ResourceUtilizationHealthCheckOptions>? configure = null,
            string name = "resource_utilization",
            HealthStatus? failureStatus = null,
            IEnumerable<string>? tags = null,
            TimeSpan? timeout = null)
        {
            ArgumentNullException.ThrowIfNull(builder);

            return builder.AddResourceUtilization(
                _ =>
                {
                    var options = new ResourceUtilizationHealthCheckOptions();
                    configure?.Invoke(options);
                    return options;
                },
                name,
                failureStatus,
                tags,
                timeout);
        }

        public static IHealthChecksBuilder AddResourceUtilization(
            this IHealthChecksBuilder builder,
            Func<IServiceProvider, ResourceUtilizationHealthCheckOptions> optionsFactory,
            string name = "resource_utilization",
            HealthStatus? failureStatus = null,
            IEnumerable<string>? tags = null,
            TimeSpan? timeout = null)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(optionsFactory);

            return builder.Add(new HealthCheckRegistration(
                name,
                sp => new ResourceUtilizationHealthCheck(optionsFactory(sp)),
                failureStatus,
                tags,
                timeout));
        }
    }
}
