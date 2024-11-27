using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using ResponseCrafter.HttpExceptions;
using SharedKernel.Constants;

namespace SharedKernel.Extensions;

public static class HealthCheckExtensions
{
   public static WebApplication EnsureHealthy(this WebApplication app)
   {
      var healthCheckService = app.Services.GetRequiredService<HealthCheckService>();
      
      var report = healthCheckService.CheckHealthAsync()
                                     .Result;

      // "masstransit-bus" entry is only becoming healthy after app.run
      var relevantEntries = report
                            .Entries
                            .Where(e => e.Key != "masstransit-bus")
                            .ToList();

      // Determine overall status based on filtered entries
      var overallStatus = relevantEntries.Exists(e => e.Value.Status == HealthStatus.Unhealthy)
         ? HealthStatus.Unhealthy
         : HealthStatus.Healthy;

      if (overallStatus != HealthStatus.Unhealthy)
      {
         return app;
      }

      var unhealthyChecks = relevantEntries
                            .Where(e => e.Value.Status != HealthStatus.Healthy)
                            .Select(e => $"{e.Key}: {e.Value.Status}")
                            .ToList();

      if (unhealthyChecks.Count == 0)
      {
         return app;
      }

      var message = $"Unhealthy services detected: {string.Join(", ", unhealthyChecks)}";
      throw new ServiceUnavailableException(message);
   }

   public static WebApplicationBuilder AddHealthChecks(this WebApplicationBuilder builder)
   {
      builder.Services.AddHealthChecks();
      return builder;
   }

   public static WebApplication MapHealthCheckEndpoints(this WebApplication app)
   {
      app
         .MapGet($"{EndpointConstants.BasePath}/ping", () => "pong")
         .Produces<string>()
         .WithTags(EndpointConstants.TagName)
         .WithOpenApi();

      app.MapHealthChecks($"{EndpointConstants.BasePath}/health",
         new HealthCheckOptions
         {
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
         });

      return app;
   }
}