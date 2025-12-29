using Microsoft.Extensions.Configuration;

namespace SharedKernel.Extensions;

internal static class ConfigurationExtensions
{
   private const string CorsOriginsConfigurationPath = "Security:AllowedCorsOrigins";
   private const string PersistentConfigurationPath = "PersistentStorage";
   private const string RepositoryNameConfigurationPath = "RepositoryName";
   private const string TimeZoneConfigurationPath = "DefaultTimeZone";

   extension(IConfiguration configuration)
   {
      internal string GetAllowedCorsOrigins()
      {
         var corsOrigins = configuration[CorsOriginsConfigurationPath];
         return corsOrigins ?? throw new InvalidOperationException("Allowed CORS origins are not configured.");
      }

      internal string GetRepositoryName()
      {
         var repositoryName = configuration[RepositoryNameConfigurationPath];
         return repositoryName ?? throw new InvalidOperationException("Repository name is not configured.");
      }

      internal string GetPersistentPath()
      {
         var persistentPath = configuration.GetConnectionString(PersistentConfigurationPath);
         return persistentPath ?? throw new InvalidOperationException("Persistent path is not configured.");
      }

      public string GetDefaultTimeZone()
      {
         var timeZone = configuration[TimeZoneConfigurationPath];
         return timeZone ?? throw new InvalidOperationException("Default time zone is not configured.");
      }
   }
}