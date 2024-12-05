using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using SharedKernel.Constants;

namespace SharedKernel.Extensions;

public static class OpenTelemetryExtension
{
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
                       .AddAspNetCoreInstrumentation()
                       .AddHttpClientInstrumentation()
                       .AddPrometheusExporter();
             })
             .WithTracing(tracing =>
             {
                tracing.AddAspNetCoreInstrumentation()
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
      builder.Services.ConfigureOpenTelemetryTracerProvider(t => t.AddOtlpExporter());

      return builder;
   }

   public static WebApplication MapPrometheusExporterEndpoints(this WebApplication app)
   {
      app.MapPrometheusScrapingEndpoint($"{EndpointConstants.BasePath}/prometheus");

      app.UseHealthChecksPrometheusExporter($"{EndpointConstants.BasePath}/prometheus/health",
         options => options.ResultStatusCodes[HealthStatus.Unhealthy] = (int)HttpStatusCode.OK);

      return app;
   }
}