using Microsoft.Extensions.Hosting;

namespace SharedKernel.Extensions;

public static class HostEnvironmentExtensions
{
   extension(IHostEnvironment hostEnvironment)
   {
      public bool IsQa()
      {
         ArgumentNullException.ThrowIfNull(hostEnvironment);

         return hostEnvironment.IsEnvironment("QA");
      }

      public bool IsLocal()
      {
         ArgumentNullException.ThrowIfNull(hostEnvironment);

         return hostEnvironment.IsEnvironment("Local");
      }

      public bool IsLocalOrDevelopment()
      {
         ArgumentNullException.ThrowIfNull(hostEnvironment);

         return hostEnvironment.IsLocal() || hostEnvironment.IsDevelopment();
      }

      public bool IsLocalOrDevelopmentOrQa()
      {
         ArgumentNullException.ThrowIfNull(hostEnvironment);

         return hostEnvironment.IsLocal() || hostEnvironment.IsDevelopment() || hostEnvironment.IsQa();
      }

      public string GetShortEnvironmentName()
      {
         ArgumentNullException.ThrowIfNull(hostEnvironment);

         if (hostEnvironment.IsLocal())
         {
            return "local";
         }

         if (hostEnvironment.IsDevelopment())
         {
            return "dev";
         }

         if (hostEnvironment.IsQa())
         {
            return "qa";
         }

         if (hostEnvironment.IsStaging())
         {
            return "staging";
         }

         return "";
      }
   }
}