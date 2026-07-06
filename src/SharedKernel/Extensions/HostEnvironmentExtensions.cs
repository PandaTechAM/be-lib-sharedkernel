using Microsoft.Extensions.Hosting;

namespace SharedKernel.Extensions;

/// <summary>
///     Provides environment predicate helpers beyond the built-in <see cref="IHostEnvironment" /> checks.
/// </summary>
public static class HostEnvironmentExtensions
{
    extension(IHostEnvironment hostEnvironment)
    {
        /// <summary>
        ///     Return whether the current environment is "QA".
        /// </summary>
        public bool IsQa()
        {
            ArgumentNullException.ThrowIfNull(hostEnvironment);

            return hostEnvironment.IsEnvironment("QA");
        }

        /// <summary>
        ///     Return whether the current environment is "Local".
        /// </summary>
        public bool IsLocal()
        {
            ArgumentNullException.ThrowIfNull(hostEnvironment);

            return hostEnvironment.IsEnvironment("Local");
        }

        /// <summary>
        ///     Return whether the current environment is "Local" or "Development".
        /// </summary>
        public bool IsLocalOrDevelopment()
        {
            ArgumentNullException.ThrowIfNull(hostEnvironment);

            return hostEnvironment.IsLocal() || hostEnvironment.IsDevelopment();
        }

        /// <summary>
        ///     Return whether the current environment is "Local", "Development", or "QA".
        /// </summary>
        public bool IsLocalOrDevelopmentOrQa()
        {
            ArgumentNullException.ThrowIfNull(hostEnvironment);

            return hostEnvironment.IsLocal() || hostEnvironment.IsDevelopment() || hostEnvironment.IsQa();
        }

        /// <summary>
        ///     Return a short lowercase name for the current environment ("local", "dev", "qa", "staging"),
        ///     or an empty string if none match.
        /// </summary>
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
