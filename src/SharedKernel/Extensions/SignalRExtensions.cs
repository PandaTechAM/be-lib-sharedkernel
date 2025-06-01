using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using ResponseCrafter.ExceptionHandlers.SignalR;
using Serilog;
using Serilog.Events;
using SharedKernel.Logging;
using StackExchange.Redis;

namespace SharedKernel.Extensions;

public static class SignalRExtensions
{
   public static WebApplicationBuilder AddSignalR(this WebApplicationBuilder builder)
   {
      builder.AddSignalRWithFiltersAndMessagePack();
      return builder;
   }

   public static WebApplicationBuilder AddDistributedSignalR(this WebApplicationBuilder builder,
      string redisUrl,
      string redisChannelName)
   {
      builder.AddSignalRWithFiltersAndMessagePack()
             .AddStackExchangeRedis(redisUrl,
                options =>
                {
                   options.Configuration.ChannelPrefix = RedisChannel.Literal(redisChannelName);
                });


      return builder;
   }

   private static ISignalRServerBuilder AddSignalRWithFiltersAndMessagePack(this WebApplicationBuilder builder)
   {
      return builder.Services
                    .AddSignalR(o =>
                    {
                       if (Log.Logger.IsEnabled(LogEventLevel.Information))
                       {
                          o.AddFilter<SignalRLoggingHubFilter>();
                       }

                       o.AddFilter<SignalRExceptionFilter>();
                    })
                    .AddMessagePackProtocol();
   }
}