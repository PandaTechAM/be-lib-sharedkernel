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
         x.UsingRabbitMq((context, cfg) =>
         {
            cfg.Host(builder.Configuration.GetConnectionString("RabbitMq")!);
            cfg.ConfigureEndpoints(context);
            cfg.UseMessageRetry(r =>
               r.Exponential(5, TimeSpan.FromSeconds(30), TimeSpan.FromHours(1), TimeSpan.FromSeconds(60)));

            //Approximate Retry Timings:
            //Retry Attempt  Approximate Delay
            //1st Retry   30 sec
            //2nd Retry   ~5 min
            //3rd Retry   ~15 min
            //4th Retry   ~30 min
            //5th Retry   ~1 hour(capped)
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
                   Uri = new Uri(rmqConnectionString),
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