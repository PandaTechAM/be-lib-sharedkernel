using System.Text.Json;
using Microsoft.AspNetCore.Http;
using System.Threading;

namespace SharedKernel.Maintenance;

internal sealed class MaintenanceMiddleware(RequestDelegate next, MaintenanceState state)
{
   private static readonly PathString[] AdminPrefixes = ["/api/admin", "/hub/admin"]; // add "/admin" if needed

   public async Task InvokeAsync(HttpContext httpContext)
   {
      var path = httpContext.Request.Path;

      // always allow health/metrics/ping and CORS preflight
      if (path.StartsWithSegments("/above-board", StringComparison.OrdinalIgnoreCase, out _) ||
          HttpMethods.IsOptions(httpContext.Request.Method))
      {
         await next(httpContext);
         return;
      }

      var mode = state.Mode;

      if (mode == MaintenanceMode.EnabledForAll)
      {
         await Set503Async(httpContext);
         return;
      }

      var isAdminRoute = AdminPrefixes.Any(p => path.StartsWithSegments(p, StringComparison.OrdinalIgnoreCase, out _));

      if (mode == MaintenanceMode.EnabledForClients && !isAdminRoute)
      {
         await Set503Async(httpContext);
         return;
      }

      await next(httpContext);
   }

   private static async Task Set503Async(HttpContext ctx)
   {
      ctx.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
      ctx.Response.Headers.RetryAfter = "60";
      ctx.Response.Headers.CacheControl = "no-store, no-cache, must-revalidate";
      ctx.Response.ContentType = "application/json; charset=utf-8";

      if (!HttpMethods.IsHead(ctx.Request.Method))
      {
         var payload = JsonSerializer.Serialize(new
         {
            message = "The service is under maintenance. Please try again later."
         });
         await ctx.Response.WriteAsync(payload);
      }
   }
}