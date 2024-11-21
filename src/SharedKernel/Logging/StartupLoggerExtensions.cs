using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Newtonsoft.Json;

namespace SharedKernel.Logging;

public static class StartupLoggerExtensions
{
   private static readonly long Stopwatch = System.Diagnostics.Stopwatch.GetTimestamp();

   public static WebApplicationBuilder LogStartAttempt(this WebApplicationBuilder builder)
   {
      var now = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
      Console.WriteLine(JsonConvert.SerializeObject(new
      {
         Timestamp = now,
         Event = "ApplicationStartAttempt",
         Application = builder.Environment.ApplicationName,
         Environment = builder.Environment.EnvironmentName
      }));
      return builder;
   }

   public static WebApplication LogStartSuccess(this WebApplication app)
   {
      var delta = System.Diagnostics.Stopwatch.GetElapsedTime(Stopwatch)
                        .TotalMilliseconds;
      var deltaInSeconds = Math.Round(delta / 1000, 2);
      var now = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
      Console.WriteLine(JsonConvert.SerializeObject(new
      {
         Timestamp = now,
         Event = "ApplicationStartSuccess",
         InitializationTime = $"{deltaInSeconds} seconds"
      }));
      return app;
   }

   public static WebApplicationBuilder LogModuleRegistrationSuccess(this WebApplicationBuilder builder,
      string moduleName)
   {
      var now = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
      Console.WriteLine(JsonConvert.SerializeObject(new
      {
         Timestamp = now,
         Event = "ModuleRegistrationSuccess",
         Module = moduleName
      }));
      return builder;
   }

   public static WebApplication LogModuleUseSuccess(this WebApplication app, string moduleName)
   {
      var now = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
      Console.WriteLine(JsonConvert.SerializeObject(new
      {
         Timestamp = now,
         Event = "ModuleUseSuccess",
         Module = moduleName
      }));
      return app;
   }
}