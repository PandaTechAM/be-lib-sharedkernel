using Elastic.CommonSchema.Serilog;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Compact;
using Serilog.Sinks.Grafana.Loki;
using SharedKernel.Extensions;

namespace SharedKernel.Logging;

public static class SerilogExtensions
{
   public static WebApplicationBuilder AddSerilog(this WebApplicationBuilder builder,
      LogBackend logBackend,
      Dictionary<string, string>? logAdditionalProperties = null,
      int daysToRetain = 7,
      bool asyncSinks = false)
   {
      builder.Logging.ClearProviders();

      var loggerConfig = new LoggerConfiguration()
                         .FilterOutUnwantedLogs()
                         .Enrich
                         .FromLogContext()
                         .ConfigureDestinations(builder, logBackend, asyncSinks)
                         .ReadFrom
                         .Configuration(builder.Configuration);

      if (logAdditionalProperties is not null)
      {
         foreach (var (key, value) in logAdditionalProperties)
         {
            loggerConfig.Enrich.WithProperty(key, value);
         }
      }

      Log.Logger = loggerConfig.CreateLogger();
      builder.Logging.AddSerilog(Log.Logger);
      builder.Services.AddSingleton(Log.Logger);

      builder.Host.UseSerilog();

      if (daysToRetain <= 0 || logBackend == LogBackend.None || builder.Environment.IsLocal())
      {
         return builder;
      }

      var logsPath = builder.GetLogsPath();
      var logDirectory = Path.GetDirectoryName(logsPath)!;
      builder.Services.AddHostedService(_ =>
         new LogCleanupHostedService(logDirectory, TimeSpan.FromDays(daysToRetain)));

      return builder;
   }


   private static LoggerConfiguration ConfigureDestinations(this LoggerConfiguration loggerConfig,
      WebApplicationBuilder builder,
      LogBackend logBackend,
      bool asyncSinks)
   {
      if (builder.Environment.IsLocal())
      {
         loggerConfig.WriteToConsole(asyncSinks);

         return loggerConfig;
      }

      if (!builder.Environment.IsProduction())
      {
         loggerConfig.WriteToConsole(asyncSinks);
      }

      if (logBackend != LogBackend.None)
      {
         loggerConfig.WriteToFile(builder, logBackend, asyncSinks);
      }

      return loggerConfig;
   }

   private static LoggerConfiguration WriteToConsole(this LoggerConfiguration loggerConfig, bool useAsync)
   {
      if (useAsync)
      {
         loggerConfig.WriteTo.Async(a => a.Console());
      }
      else
      {
         loggerConfig.WriteTo.Console();
      }

      return loggerConfig;
   }

   private static LoggerConfiguration WriteToFile(this LoggerConfiguration loggerConfig,
      WebApplicationBuilder builder,
      LogBackend logBackend,
      bool useAsync)
   {
      // Choose the formatter based on the selected log backend
      ITextFormatter formatter = logBackend switch
      {
         LogBackend.ElasticSearch => new EcsTextFormatter(),
         LogBackend.Loki => new LokiJsonTextFormatter(),
         LogBackend.CompactJson => new CompactJsonFormatter(),
         _ => new CompactJsonFormatter() // Fallback
      };

      var logPath = builder.GetLogsPath();

      if (useAsync)
      {
         return loggerConfig.WriteTo.Async(a =>
               a.File(formatter,
                  logPath,
                  rollingInterval: RollingInterval.Day),
            blockWhenFull: true,
            bufferSize: 20_000);
      }

      return loggerConfig.WriteTo.File(formatter, logPath, rollingInterval: RollingInterval.Day);
   }

   private static LoggerConfiguration FilterOutUnwantedLogs(this LoggerConfiguration loggerConfig)
   {
      return loggerConfig
             .Filter
             .ByExcluding(e => e.ExcludeOutboxAndMassTransit())
             .Filter
             .ByExcluding(evt =>
             {
                if (!evt.Properties.TryGetValue("RequestPath", out var p) || p is not ScalarValue sv)
                   return false;
                var path = sv.Value as string ?? "";
                return path.StartsWith("/swagger")
                       || path.StartsWith("/hangfire")
                       || path.Contains("/above-board")
                       || path.Contains("localhost/auth/is-authenticated.json?api-version=v2");
             });
   }

   private static bool ExcludeOutboxAndMassTransit(this LogEvent logEvent)
   {
      var message = logEvent.RenderMessage();
      return message.Contains("outbox_messages", StringComparison.OrdinalIgnoreCase) || message.StartsWith(
         "Health check masstransit-bus with status Unhealthy completed after",
         StringComparison.OrdinalIgnoreCase);
   }

   private static string GetLogsPath(this WebApplicationBuilder builder)
   {
      var persistencePath = builder.Configuration.GetPersistentPath();
      var repoName = builder.Configuration.GetRepositoryName();
      var envName = builder.Environment.GetShortEnvironmentName();
      var instanceId = Guid.NewGuid()
                           .ToString();
      var fileName = $"logs-{instanceId}-.json";
      return Path.Combine(persistencePath, repoName, envName, "logs", fileName);
   }
}