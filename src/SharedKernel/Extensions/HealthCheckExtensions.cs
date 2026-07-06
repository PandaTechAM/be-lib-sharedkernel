using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using ResponseCrafter.HttpExceptions;
using SharedKernel.Constants;

namespace SharedKernel.Extensions;

/// <summary>
///     Provides health check registration, endpoint mapping, and startup health verification.
/// </summary>
public static class HealthCheckExtensions
{
    /// <summary>
    ///     Run all health checks except the MassTransit bus check and throw <see cref="ServiceUnavailableException" />
    ///     if any are unhealthy, blocking a broken instance from starting.
    /// </summary>
    public static WebApplication EnsureHealthy(this WebApplication app)
    {
        var svc = app.Services.GetRequiredService<HealthCheckService>();

        // Skip MassTransit during preflight
        var report = svc.CheckHealthAsync(r => r.Name != "masstransit-bus")
            .GetAwaiter()
            .GetResult();

        var failures = report.Entries
            .Where(e => e.Value.Status == HealthStatus.Unhealthy)
            .Select(e => $"{e.Key}: {e.Value.Status}")
            .ToList();

        return failures.Count > 0
            ? throw new ServiceUnavailableException($"Unhealthy services detected: {string.Join(", ", failures)}")
            : app;
    }

    /// <summary>
    ///     Register the health checks service.
    /// </summary>
    public static WebApplicationBuilder AddHealthChecks(this WebApplicationBuilder builder)
    {
        builder.Services.AddHealthChecks();
        return builder;
    }

    /// <summary>
    ///     Map the ping, health, and detailed health check endpoints under <see cref="EndpointConstants.BasePath" />.
    /// </summary>
    public static WebApplication MapHealthCheckEndpoints(this WebApplication app)
    {
        app
            .MapGet($"{EndpointConstants.BasePath}/ping", () => "pong")
            .Produces<string>()
            .WithTags(EndpointConstants.TagName);

        app.MapHealthChecks($"{EndpointConstants.BasePath}/health");

        app.MapHealthChecks($"{EndpointConstants.BasePath}/health/detailed",
            new HealthCheckOptions
            {
                ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
            });

        return app;
    }
}
