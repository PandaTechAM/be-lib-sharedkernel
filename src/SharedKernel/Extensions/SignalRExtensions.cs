using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using ResponseCrafter.ExceptionHandlers.SignalR;
using Serilog;
using Serilog.Events;
using SharedKernel.Logging.Middleware;
using StackExchange.Redis;

namespace SharedKernel.Extensions;

public static class SignalRExtensions
{
   extension(WebApplicationBuilder builder)
   {
      public WebApplicationBuilder AddSignalR()
      {
         builder.AddSignalRWithFiltersAndMessagePack();
         return builder;
      }

      public WebApplicationBuilder AddDistributedSignalR(string redisUrl,
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

      private ISignalRServerBuilder AddSignalRWithFiltersAndMessagePack()
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
}