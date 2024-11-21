using DistributedCache.Extensions;
using DistributedCache.Options;
using Microsoft.AspNetCore.Builder;

namespace SharedKernel.Extensions;

public static class DistributedCacheExtension
{
   public static WebApplicationBuilder AddRedis(this WebApplicationBuilder builder, KeyPrefix keyPrefix)
   {
      builder.AddDistributedCache(options =>
      {
         options.RedisConnectionString = builder.Configuration.GetRedisUrl();
         options.KeyPrefixForIsolation = keyPrefix;
      });

      return builder;
   }
}