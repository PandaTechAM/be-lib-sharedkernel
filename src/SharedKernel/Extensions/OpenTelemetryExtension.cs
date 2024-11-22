using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
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
                       .AddHttpClientInstrumentation();
             });

      return builder;
   }

   public static WebApplication MapPrometheusExporterEndpoints(this WebApplication app)
   {
      app.MapPrometheusScrapingEndpoint($"{EndpointConstants.BasePath}/metrics");

      app.UseHealthChecksPrometheusExporter($"{EndpointConstants.BasePath}/metrics/health",
         options => options.ResultStatusCodes[HealthStatus.Unhealthy] = (int)HttpStatusCode.OK);

      return app;
   }
}