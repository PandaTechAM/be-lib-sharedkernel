using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Prometheus;
using SharedKernel.Constants;

namespace SharedKernel.Extensions;

/// <summary>
///     Extension methods for registering OpenTelemetry instrumentation and exposing the Prometheus scrape endpoint.
/// </summary>
public static class OpenTelemetryExtension
{
    /// <summary>
    ///     Registers OpenTelemetry logging, metrics, and tracing instrumentation, enabling OTLP export when the
    ///     <c>OTEL_EXPORTER_OTLP_ENDPOINT</c> configuration value is set.
    /// </summary>
    public static WebApplicationBuilder AddOpenTelemetry(this WebApplicationBuilder builder)
    {
        builder.Logging.AddOpenTelemetry(x =>
        {
            x.IncludeScopes = true;
            x.IncludeFormattedMessage = true;
        });

        builder.Services
            .AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(builder.Environment.ApplicationName))
            .WithMetrics(metrics =>
            {
                metrics.AddRuntimeInstrumentation()
                    //  .AddFusionCacheInstrumentation()
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddPrometheusExporter();
            })
            .WithTracing(tracing =>
            {
                tracing
                    //    .AddFusionCacheInstrumentation()
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddEntityFrameworkCoreInstrumentation();
            });

        var otlpEnabled = !string.IsNullOrEmpty(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

        if (!otlpEnabled)
        {
            return builder;
        }

        builder.Services.ConfigureOpenTelemetryLoggerProvider(l => l.AddOtlpExporter());
        builder.Services.ConfigureOpenTelemetryTracerProvider(t => t.AddOtlpExporter());

        return builder;
    }

    /// <summary>
    ///     Suppresses prometheus-net's default Meter/EventCounter mirroring and maps the Prometheus metrics
    ///     scraping and health exporter endpoints.
    /// </summary>
    public static WebApplication MapPrometheusExporterEndpoints(this WebApplication app)
    {
        // prometheus-net (transitive via AspNetCore.HealthChecks.Prometheus.Metrics) auto-mirrors every
        // .NET Meter and EventCounter onto its registry and locks in the first-seen tag layout per
        // instrument; instruments with varying tag sets (e.g. Npgsql command metrics) then throw a
        // swallowed ArgumentException on every measurement (~90/s per app). The OTel scraping endpoint
        // above already exposes these metrics, so keep only the explicitly registered health gauges.
        Metrics.SuppressDefaultMetrics(new SuppressDefaultMetricOptions
        {
            SuppressEventCounters = true,
            SuppressMeters = true
        });

        app.MapPrometheusScrapingEndpoint($"{EndpointConstants.BasePath}/prometheus");

        app.UseHealthChecksPrometheusExporter($"{EndpointConstants.BasePath}/prometheus/health",
            options => options.ResultStatusCodes[HealthStatus.Unhealthy] = (int)HttpStatusCode.OK);

        return app;
    }
}
