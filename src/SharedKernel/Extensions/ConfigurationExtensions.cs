using Microsoft.Extensions.Configuration;

namespace SharedKernel.Extensions;

internal static class ConfigurationExtensions
{
   private const string CorsOriginsConfigurationPath = "Security:AllowedCorsOrigins";
   private const string PersistentConfigurationPath = "PersistentStorage";
   private const string RepositoryNameConfigurationPath = "RepositoryName";
   private const string TimeZoneConfigurationPath = "DefaultTimeZone";
   private const string RedisConfigurationPath = "Redis";
   private const string LokiConfigurationPath = "Loki";

   internal static string GetAllowedCorsOrigins(this IConfiguration configuration)
   {
      var corsOrigins = configuration[CorsOriginsConfigurationPath];
      if (corsOrigins is null)
      {
         throw new InvalidOperationException("Allowed CORS origins are not configured.");
      }

      return corsOrigins;
   }

   internal static string GetRepositoryName(this IConfiguration configuration)
   {
      var repositoryName = configuration[RepositoryNameConfigurationPath];
      if (repositoryName is null)
      {
         throw new InvalidOperationException("Repository name is not configured.");
      }

      return repositoryName;
   }

   internal static string GetPersistentPath(this IConfiguration configuration)
   {
      var persistentPath = configuration.GetConnectionString(PersistentConfigurationPath);
      if (persistentPath is null)
      {
         throw new InvalidOperationException("Persistent path is not configured.");
      }

      return persistentPath;
   }

   public static string GetDefaultTimeZone(this IConfiguration configuration)
   {
      var timeZone = configuration[TimeZoneConfigurationPath];
      if (timeZone is null)
      {
         throw new InvalidOperationException("Default time zone is not configured.");
      }

      return timeZone;
   }

   public static string GetRedisUrl(this IConfiguration configuration)
   {
      var redisConnectionString = configuration.GetConnectionString(RedisConfigurationPath);
      if (redisConnectionString is null)
      {
         throw new InvalidOperationException("Redis connection string is not configured.");
      }

      return redisConnectionString;
   }

   public static string GetLokiUrl(this IConfiguration configuration)
   {
      var lokiUrl = configuration.GetConnectionString(LokiConfigurationPath);
      
      if (lokiUrl is null)
      {
         throw new InvalidOperationException("Loki URL is not configured.");
      }

      return lokiUrl;
   }
}