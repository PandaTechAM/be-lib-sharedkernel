using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.DependencyInjection;
using ZiggyCreatures.Caching.Fusion;

namespace SharedKernel.Extensions;

public static class FusionCacheExtensions
{
   private static IFusionCacheBuilder AddBaseFusionCache(WebApplicationBuilder builder, string instanceName)
   {
      return builder.Services
                    .AddFusionCache()
                    .WithRegisteredLogger()
                    .WithNeueccMessagePackSerializer()
                    .WithDefaultEntryOptions(new FusionCacheEntryOptions())
                    .WithCacheKeyPrefix(instanceName);
   }


   public static WebApplicationBuilder AddDistributedFusionCache(this WebApplicationBuilder builder,
      string redisUrl,
      string instanceName)
   {
      AddBaseFusionCache(builder, instanceName)
         .WithDistributedCache(new RedisCache(new RedisCacheOptions
         {
            Configuration = redisUrl,
            InstanceName = instanceName
         }))
         .WithStackExchangeRedisBackplane(o => o.Configuration = redisUrl)
         .AsHybridCache();

      return builder;
   }

   public static WebApplicationBuilder AddFusionCache(this WebApplicationBuilder builder, string instanceName)
   {
      AddBaseFusionCache(builder, instanceName);
      return builder;
   }
}