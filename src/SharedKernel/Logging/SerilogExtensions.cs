using System.Globalization;
using Elastic.CommonSchema;
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
using Log = Serilog.Log;

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
      var ecsConfig = new EcsTextFormatterConfiguration
      {
         MessageFormatProvider = CultureInfo.InvariantCulture,
         IncludeHost = false,
         IncludeProcess = false,
         IncludeUser = false,

         MapCustom = (doc, _) =>
         {
            doc.Agent = null;

            if (doc.Labels is { Count: > 0 })
            {
               doc.Labels.Remove("MessageTemplate");
            }

            if (doc.Event != null)
            {
               var dur = doc.Event.Duration;
               doc.Event = new Event
               {
                  Duration = dur
               };
            }

            if (doc.Service != null)
            {
               doc.Service.Type = null;
            }

            return doc;
         }
      };
      ITextFormatter formatter = logBackend switch
      {
         LogBackend.ElasticSearch => new EcsTextFormatter(ecsConfig),
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
            bufferSize: 10_000);
      }

      return loggerConfig.WriteTo.File(formatter, logPath, rollingInterval: RollingInterval.Day);
   }

   private static LoggerConfiguration FilterOutUnwantedLogs(this LoggerConfiguration loggerConfig)
   {
      return loggerConfig
             .Filter
             .ByExcluding(IsEfOutboxQuery)
             .Filter
             .ByExcluding(ShouldDropByPath);
   }

   private static bool ShouldDropByPath(LogEvent evt)
   {
      if (!evt.Properties.TryGetValue("RequestPath", out var p) || p is not ScalarValue sv)
      {
         return false;
      }

      var path = sv.Value as string ?? "";

      return path.StartsWith("/swagger")
             || path.StartsWith("/hangfire")
             || path.Contains("/above-board")
             || path.Contains("localhost/auth/is-authenticated.json?api-version=v2");
   }

   private static bool IsEfOutboxQuery(LogEvent evt)
   {
      // Only EF Core command category
      if (!(evt.Properties.TryGetValue("SourceContext", out var sc) &&
            sc is ScalarValue sv && sv.Value is string src &&
            src.Contains("Microsoft.EntityFrameworkCore.Database.Command", StringComparison.OrdinalIgnoreCase)))
      {
         return false;
      }

      var sql = Get(evt, "commandText") ?? Get(evt, "CommandText");

      return !string.IsNullOrEmpty(sql) &&
             // Match table references, regardless of quoting/schema
             // e.g. outbox_messages, "outbox_messages", [outbox_messages], public.outbox_messages
             sql.Contains("outbox_messages", StringComparison.OrdinalIgnoreCase);

      // Grab the structured SQL (EF logs it as 'commandText'; some sinks rename to 'CommandText')
      static string? Get(LogEvent e, string name) =>
         e.Properties.TryGetValue(name, out var v) && v is ScalarValue s && s.Value is string str ? str : null;
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