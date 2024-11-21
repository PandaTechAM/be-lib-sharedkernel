using System.Net;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace SharedKernel.Extensions;

public static class DefaultEndpoints
{
   private const string TagName = "above-board";
   private const string BasePath = $"/{TagName}";

   public static WebApplication MapDefaultEndpoints(this WebApplication app)
   {
      app
         .MapPingEndpoint()
         .MapPrometheusEndpoints()
         .MapHealthEndpoint();
      return app;
   }

   private static WebApplication MapPrometheusEndpoints(this WebApplication app)
   {
      app.MapPrometheusScrapingEndpoint($"{BasePath}/metrics");

      app.UseHealthChecksPrometheusExporter($"{BasePath}/metrics/health",
         options => options.ResultStatusCodes[HealthStatus.Unhealthy] = (int)HttpStatusCode.OK);

      return app;
   }

   private static WebApplication MapPingEndpoint(this WebApplication app)
   {
      app
         .MapGet($"{BasePath}/ping", () => "pong")
         .Produces<string>()
         .WithTags(TagName)
         .WithOpenApi();
      return app;
   }

   private static WebApplication MapHealthEndpoint(this WebApplication app)
   {
      app.MapHealthChecks($"{BasePath}/health",
         new HealthCheckOptions
         {
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
         });

      return app;
   }
}