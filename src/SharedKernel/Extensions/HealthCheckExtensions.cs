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