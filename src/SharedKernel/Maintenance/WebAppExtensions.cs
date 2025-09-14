using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel.Constants;

namespace SharedKernel.Maintenance;

public static class WebAppExtensions
{
   private static bool _builderAdded;
   private static bool _webAppAdded;

   public static WebApplicationBuilder AddMaintenanceMode(this WebApplicationBuilder builder)
   {
      if (_builderAdded)
      {
         return builder;
      }

      builder.Services.AddSingleton<MaintenanceState>();
      builder.Services.AddHostedService<MaintenanceCachePoller>();

      _builderAdded = true;
      return builder;
   }

   public static WebApplication UseMaintenanceMode(this WebApplication app)
   {
      if (_webAppAdded)
      {
         return app;
      }

      if (!_builderAdded)
      {
         throw new InvalidOperationException(
            "You must call AddMaintenanceMode on the WebApplicationBuilder before calling UseMaintenanceMode on the WebApplication.");
      }

      app.UseMiddleware<MaintenanceMiddleware>();

      _webAppAdded = true;

      return app;
   }

   /// <summary>
   ///    Maps PUT {EndpointConstants.BasePath}/maintenance.
   ///    Set <paramref name="querySecret" /> to enable a shared-secret check; leave it null to use your own authorization
   ///    instead.
   /// </summary>
   public static RouteHandlerBuilder MapMaintenanceEndpoint(this IEndpointRouteBuilder app,
      string? querySecret = null)
   {
      if (!_webAppAdded)
      {
         throw new InvalidOperationException("Call UseMaintenanceMode before MapMaintenanceEndpoint.");
      }


      if (string.IsNullOrWhiteSpace(querySecret))
      {
         return app.MapPut(EndpointConstants.BasePath + "/maintenance",
                      async ([FromServices] MaintenanceState state,
                         [FromBody] MaintenanceModeRequest req,
                         CancellationToken ct) =>
                      {
                         await state.SetModeAsync(req.Mode, ct);
                         return TypedResults.Ok(new MaintenanceModeResponse("Mode set to " + req.Mode,
                            DateTimeOffset.UtcNow));
                      })
                   .WithTags(EndpointConstants.TagName)
                   .WithSummary("Set maintenance mode");
      }

      return app.MapPut(EndpointConstants.BasePath + "/maintenance",
                   async ([FromServices] MaintenanceState state,
                      [FromBody] MaintenanceModeRequest req,
                      [FromQuery] string secret,
                      CancellationToken ct) =>
                   {
                      if (!string.Equals(secret, querySecret, StringComparison.Ordinal))
                      {
                         return Results.Unauthorized();
                      }

                      await state.SetModeAsync(req.Mode, ct);
                      return TypedResults.Ok(new MaintenanceModeResponse("Mode set to " + req.Mode,
                         DateTimeOffset.UtcNow));
                   })
                .WithTags(EndpointConstants.TagName)
                .WithSummary("Set maintenance mode")
                .Produces(StatusCodes.Status401Unauthorized);
   }
}

public sealed record MaintenanceModeRequest(MaintenanceMode Mode);

public sealed record MaintenanceModeResponse(string Message, DateTimeOffset UpdatedAt);