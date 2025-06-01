using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;

namespace SharedKernel.Logging;

public static class LoggingExtensions
{
   public static WebApplication UseRequestLogging(this WebApplication app)
   {
      if (Log.Logger.IsEnabled(LogEventLevel.Information))
      {
         app.UseMiddleware<RequestLoggingMiddleware>();
      }

      return app;
   }

   public static WebApplicationBuilder AddOutboundLoggingHandler(this WebApplicationBuilder builder)
   {
      if (Log.Logger.IsEnabled(LogEventLevel.Information))
      {
         builder.Services.AddTransient<OutboundLoggingHandler>();
      }

      return builder;
   }

   public static IHttpClientBuilder AddOutboundLoggingHandler(this IHttpClientBuilder builder)
   {
      if (Log.Logger.IsEnabled(LogEventLevel.Information))
      {
         builder.AddHttpMessageHandler<OutboundLoggingHandler>();
      }

      return builder;
   }
}