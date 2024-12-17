using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using ResponseCrafter.ExceptionHandlers.SignalR;
using StackExchange.Redis;

namespace SharedKernel.Extensions;

public static class SignalRExtensions
{
   
   public static WebApplicationBuilder AddSignalR(this WebApplicationBuilder builder)
   {
      builder
         .Services
         .AddSignalR(o => o.AddFilter<SignalRExceptionFilter>())
         .AddMessagePackProtocol();

      return builder;
   }
   
   public static WebApplicationBuilder AddDistributedSignalR(this WebApplicationBuilder builder, string redisChannelName)
   {
      builder
         .Services
         .AddSignalR(o => o.AddFilter<SignalRExceptionFilter>())
         .AddMessagePackProtocol()
         .AddStackExchangeRedis(builder.Configuration.GetRedisUrl(),
            options =>
            {
               options.Configuration.ChannelPrefix = RedisChannel.Literal("FinHub:SignalR:");
            });


      return builder;
   }
}