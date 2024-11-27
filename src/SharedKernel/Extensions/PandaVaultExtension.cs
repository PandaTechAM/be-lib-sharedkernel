using Microsoft.AspNetCore.Builder;
using PandaVaultClient.Extensions;

namespace SharedKernel.Extensions;

public static class PandaVaultExtension
{
   public static WebApplicationBuilder ConfigureWithPandaVault(this WebApplicationBuilder builder)
   {
      if (!builder.Environment.IsLocal())
      {
         builder.AddPandaVault();
      }

      return builder;
   }
}