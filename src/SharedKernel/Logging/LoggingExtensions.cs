using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;

namespace SharedKernel.Logging;

public static class LoggingExtensions
{
   public static WebApplication UseRequestResponseLogging(this WebApplication app)
   {
      if (app.Logger.IsEnabled(LogLevel.Information))
      {
         app.UseMiddleware<RequestResponseLoggingMiddleware>();
      }

      return app;
   }
}