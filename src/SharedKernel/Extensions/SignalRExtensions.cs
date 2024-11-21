using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace SharedKernel.Extensions;

public static class SignalRExtensions
{
   
   public static WebApplicationBuilder AddSignalR(this WebApplicationBuilder builder)
   {
      builder
         .Services
         .AddSignalR()
         .AddMessagePackProtocol();

      return builder;
   }
   
   public static WebApplicationBuilder AddDistributedSignalR(this WebApplicationBuilder builder, string redisChannelName)
   {
      builder
         .Services
         .AddSignalR()
         .AddMessagePackProtocol()
         .AddStackExchangeRedis(builder.Configuration.GetRedisUrl(),
            options =>
            {
               options.Configuration.ChannelPrefix = RedisChannel.Literal("FinHub:SignalR:");
            });


      return builder;
   }
}