using Microsoft.AspNetCore.Builder;
using PandaVaultClient.Extensions;

namespace SharedKernel.Extensions;

/// <summary>
///     Extension methods for wiring up PandaVault secret configuration.
/// </summary>
public static class PandaVaultExtension
{
    /// <summary>
    ///     Configures PandaVault for the application, skipping it entirely on local environments.
    /// </summary>
    public static WebApplicationBuilder ConfigureWithPandaVault(this WebApplicationBuilder builder)
    {
        if (!builder.Environment.IsLocal())
        {
            builder.AddPandaVault();
        }

        return builder;
    }
}
