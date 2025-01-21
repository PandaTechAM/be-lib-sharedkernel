using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace SharedKernel.Logging;

public static class LoggingExtensions
{
   public static WebApplication UseRequestLogging(this WebApplication app)
   {
      if (app.Logger.IsEnabled(LogLevel.Information))
      {
         app.UseMiddleware<RequestLoggingMiddleware>();
      }

      return app;
   }

   public static WebApplicationBuilder AddOutboundLoggingHandler(this WebApplicationBuilder builder)
   {
      builder.Services.AddTransient<OutboundLoggingHandler>();
      
      return builder;
   }
   
   public static IHttpClientBuilder AddOutboundLoggingHandler(this IHttpClientBuilder builder)
   {
      builder.AddHttpMessageHandler<OutboundLoggingHandler>();
      return builder;
   }
}