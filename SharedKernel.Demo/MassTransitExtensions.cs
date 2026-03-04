using System.Reflection;
using MassTransit;
using RabbitMQ.Client;

namespace SharedKernel.Demo;

public static class MassTransitExtension
{
   public static WebApplicationBuilder AddMassTransit(this WebApplicationBuilder builder, params Assembly[] assemblies)
   {
      builder.Services.AddMassTransit(x =>
      {
         x.AddConsumers(assemblies);
         x.SetKebabCaseEndpointNameFormatter();

         // Quorum queue for HA — apply regardless of env so staging mirrors prod.
         // Degrades gracefully on single-node (still works, just no replication).
         x.AddConfigureEndpointsCallback((_, cfg) =>
         {
            if (cfg is IRabbitMqReceiveEndpointConfigurator rmq && builder.Environment.IsProduction())
            {
               rmq.SetQuorumQueue(3);
            }
         });

         x.UsingRabbitMq((context, cfg) =>
         {
            cfg.Host(builder.Configuration.GetConnectionString("RabbitMq")!);

            // Required for UseDelayedRedelivery to bind to the RabbitMQ delayed exchange plugin.
            cfg.UseDelayedMessageScheduler();

            // Outer: delayed redelivery — survives 3rd-party outages.
            // After exhaustion → *_error queue for manual intervention / alerting.
            cfg.UseDelayedRedelivery(r => r.Intervals(
               TimeSpan.FromMinutes(5),
               TimeSpan.FromMinutes(30),
               TimeSpan.FromHours(2),
               TimeSpan.FromHours(6),
               TimeSpan.FromHours(12),
               TimeSpan.FromHours(24)));

            // Inner: immediate in-process retries for transient faults (network blip, deadlock).
            // Keep low — expensive failures should flow to delayed redelivery fast.
            cfg.UseMessageRetry(r =>
               r.Exponential(4, TimeSpan.FromMilliseconds(200), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(2)));

            // Tune based on consumer count and message processing time. Higher → better throughput but more messages in-flight (potentially out of order).
            cfg.PrefetchCount = 32;

            cfg.ConfigureEndpoints(context);
         });
      });

      builder.AddRmqHealthCheck();
      return builder;
   }
}

internal static class RmqHealthCheckExtensions
{
   public static WebApplicationBuilder AddRmqHealthCheck(this WebApplicationBuilder builder)
   {
      builder.Services
             .AddSingleton<IConnection>(_ =>
             {
                var rmqConnectionString = builder.Configuration.GetConnectionString("RabbitMq")!;
                var factory = new ConnectionFactory
                {
                   Uri = new Uri(rmqConnectionString)
                };

                return factory.CreateConnectionAsync()
                              .GetAwaiter()
                              .GetResult();
             })
             .AddHealthChecks()
             .AddRabbitMQ(name: "rabbit_mq", timeout: TimeSpan.FromSeconds(3), tags: ["rmq"]);

      return builder;
   }
}