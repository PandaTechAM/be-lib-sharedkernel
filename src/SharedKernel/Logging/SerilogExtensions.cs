using Elastic.CommonSchema.Serilog;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.Grafana.Loki;
using SharedKernel.Extensions;

namespace SharedKernel.Logging;

public static class SerilogExtensions
{
   public static WebApplicationBuilder AddSerilog(this WebApplicationBuilder builder,
      LogBackend logBackend,
      int daysToRetain = 7,
      Dictionary<string, string>? valueByNameRepeatedLog = null)
   {
      builder.Logging.ClearProviders();

      var loggerConfig = new LoggerConfiguration()
                         .FilterOutUnwantedLogs()
                         .Enrich
                         .FromLogContext()
                         .ConfigureDestinations(builder, logBackend)
                         .ReadFrom
                         .Configuration(builder.Configuration);

      if (valueByNameRepeatedLog is not null)
      {
         foreach (var (key, value) in valueByNameRepeatedLog)
         {
            loggerConfig.Enrich.WithProperty(key, value);
         }
      }

      Log.Logger = loggerConfig.CreateLogger();
      builder.Logging.AddSerilog(Log.Logger);
      builder.Services.AddSingleton(Log.Logger);

      builder.Host.UseSerilog();

      if (daysToRetain <= 0 || logBackend == LogBackend.None)
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
      LogBackend logBackend)
   {
      if (builder.Environment.IsLocal())
      {
         loggerConfig.WriteTo.Async(a => a.Console());
         return loggerConfig;
      }

      if (!builder.Environment.IsProduction())
      {
         loggerConfig.WriteTo.Async(a => a.Console());
      }

      switch (logBackend)
      {
         case LogBackend.None:
            break;

         case LogBackend.ElasticSearch:
            loggerConfig.WriteToEcsFileAsync(builder);
            break;

         case LogBackend.Loki:
            loggerConfig.WriteToLokiAsync(builder);
            break;
         default:
            throw new ArgumentOutOfRangeException(nameof(logBackend), logBackend, null);
      }

      return loggerConfig;
   }

   private static LoggerConfiguration WriteToEcsFileAsync(this LoggerConfiguration loggerConfig,
      WebApplicationBuilder builder)
   {
      return loggerConfig.WriteTo.Async(a =>
         a.File(
            new EcsTextFormatter(),
            builder.GetLogsPath(),
            rollingInterval: RollingInterval.Day
         )
      );
   }

   private static LoggerConfiguration WriteToLokiAsync(this LoggerConfiguration loggerConfig,
      WebApplicationBuilder builder)
   {
      return loggerConfig.WriteTo.Async(a =>
         a.File(
            new LokiJsonTextFormatter(),
            builder.GetLogsPath(),
            rollingInterval: RollingInterval.Day
         )
      );
   }

   #region Filtering

   private static LoggerConfiguration FilterOutUnwantedLogs(this LoggerConfiguration loggerConfig)
   {
      return loggerConfig
             .Filter
             .ByExcluding(e => e.ShouldExcludeHangfireDashboardLogs())
             .Filter
             .ByExcluding(e => e.ShouldExcludeOutboxDbCommandLogs())
             .Filter
             .ByExcluding(e => e.ShouldExcludeSwaggerLogs())
             .Filter
             .ByExcluding(e => e.ShouldExcludeMassTransitHealthCheckLogs());
   }

   private static bool ShouldExcludeHangfireDashboardLogs(this LogEvent logEvent)
   {
      return logEvent.Properties.TryGetValue("RequestPath", out var requestPathValue)
             && requestPathValue is ScalarValue requestPath
             && requestPath.Value
                           ?.ToString()
                           ?.Contains("/hangfire") == true;
   }

   private static bool ShouldExcludeOutboxDbCommandLogs(this LogEvent logEvent)
   {
      var message = logEvent.RenderMessage();
      return message.Contains("outbox_messages", StringComparison.OrdinalIgnoreCase);
   }

   private static bool ShouldExcludeSwaggerLogs(this LogEvent logEvent)
   {
      return logEvent.Properties.TryGetValue("RequestPath", out var requestPathValue)
             && requestPathValue is ScalarValue requestPath
             && requestPath.Value
                           ?.ToString()
                           ?.Contains("/swagger") == true;
   }

   private static bool ShouldExcludeMassTransitHealthCheckLogs(this LogEvent logEvent)
   {
      var message = logEvent.RenderMessage();
      return message.Contains("Health check masstransit-bus", StringComparison.OrdinalIgnoreCase)
             && message.Contains("Unhealthy", StringComparison.OrdinalIgnoreCase)
             && message.Contains("Not ready: not started", StringComparison.OrdinalIgnoreCase);
   }

   #endregion

   #region File Path Helper

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

   #endregion
}