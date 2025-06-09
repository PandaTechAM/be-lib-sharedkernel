using System.Diagnostics;
using System.Globalization;
using Microsoft.AspNetCore.Builder;

namespace SharedKernel.Logging;

public static class StartupLoggerExtensions
{
   private static long? _startTimestamp = null;

   public static WebApplicationBuilder LogStartAttempt(this WebApplicationBuilder builder)
   {
      _startTimestamp = Stopwatch.GetTimestamp();
      var now = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);

      Console.ForegroundColor = ConsoleColor.DarkYellow;
      Console.WriteLine("===============================================================");
      Console.ResetColor();

      Console.WriteLine("APPLICATION START ATTEMPT");
      Console.WriteLine($"Timestamp   : {now}");
      Console.WriteLine($"Application : {builder.Environment.ApplicationName}");
      Console.WriteLine($"Environment : {builder.Environment.EnvironmentName}");
      Console.WriteLine($"OS Version  : {Environment.OSVersion}");
      Console.WriteLine($"Machine Name : {Environment.MachineName}");

      Console.ForegroundColor = ConsoleColor.DarkYellow;
      Console.WriteLine("===============================================================");
      Console.ResetColor();

      return builder;
   }

   public static WebApplication LogStartSuccess(this WebApplication app)
   {
      var delta = Stopwatch.GetElapsedTime((long)_startTimestamp!)
                           .TotalMilliseconds;
      var deltaInSeconds = Math.Round(delta / 1000, 2);
      var now = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);

      Console.ForegroundColor = ConsoleColor.DarkGreen;
      Console.WriteLine("\n===============================================================");
      Console.ResetColor();

      Console.WriteLine("APPLICATION START SUCCESS");
      Console.WriteLine($"Timestamp         : {now}");
      Console.WriteLine($"Initialization    : {deltaInSeconds} seconds");

      Console.ForegroundColor = ConsoleColor.DarkGreen;
      Console.WriteLine("===============================================================");
      Console.ResetColor();

      return app;
   }

   public static WebApplicationBuilder LogModuleRegistrationSuccess(this WebApplicationBuilder builder,
      string moduleName)
   {
      var now = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);

      Console.ForegroundColor = ConsoleColor.DarkYellow;
      Console.WriteLine("\n===============================================================");
      Console.ResetColor();

      Console.WriteLine("MODULE REGISTRATION SUCCESS");
      Console.WriteLine($"Timestamp   : {now}");
      Console.WriteLine($"Module Name : {moduleName}");

      Console.ForegroundColor = ConsoleColor.DarkYellow;
      Console.WriteLine("===============================================================");
      Console.ResetColor();

      return builder;
   }

   public static WebApplication LogModuleUseSuccess(this WebApplication app, string moduleName)
   {
      var now = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);

      Console.ForegroundColor = ConsoleColor.DarkYellow;
      Console.WriteLine("\n===============================================================");
      Console.ResetColor();

      Console.WriteLine("MODULE USE SUCCESS");
      Console.WriteLine($"Timestamp   : {now}");
      Console.WriteLine($"Module Name : {moduleName}");

      Console.ForegroundColor = ConsoleColor.DarkYellow;
      Console.WriteLine("===============================================================");
      Console.ResetColor();

      return app;
   }
}