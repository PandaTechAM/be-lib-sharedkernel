using Microsoft.AspNetCore.Builder;
using PandaVaultClient;

namespace SharedKernel.Extensions;

public static class PandaVaultExtension
{
   public static WebApplicationBuilder AddPandaVault(this WebApplicationBuilder builder)
   {
      if (!builder.Environment.IsLocal())
      {
         builder.Configuration.AddPandaVault();
      }

      return builder;
   }
}