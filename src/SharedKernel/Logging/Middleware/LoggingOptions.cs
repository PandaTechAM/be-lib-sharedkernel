using System.Collections.Frozen;

namespace SharedKernel.Logging.Middleware;

/// <summary>
///     Static configuration limits and lookup sets used by the request/response logging middleware.
/// </summary>
public static class LoggingOptions
{
    /// <summary>
    ///     Maximum number of bytes of a request or response body captured for logging.
    /// </summary>
    public const int RequestResponseBodyMaxBytes = 16 * 1024;

    /// <summary>
    ///     Maximum number of bytes of a redacted property value retained in logs.
    /// </summary>
    public const int RedactionMaxPropertyBytes = 2 * 1024;

    /// <summary>
    ///     Sensitive keywords to redact in headers and JSON bodies.
    ///     Uses FrozenSet for optimized lookups on static data.
    /// </summary>
    public static readonly FrozenSet<string> SensitiveKeywords = new HashSet<string>(
        [
            "pwd", "pass", "secret", "token", "cookie", "auth",
            "pan", "cvv", "cvc", "cardholder", "bindingid",
            "ssn", "tin", "iban", "swift", "bankaccount", "notboundcard"
        ],
        StringComparer.OrdinalIgnoreCase).ToFrozenSet();

    /// <summary>
    ///     Media type prefixes considered text-like for logging purposes.
    /// </summary>
    public static readonly FrozenSet<string> TextLikeMediaPrefixes = new HashSet<string>(
        [
            "application/json",
            "application/x-www-form-urlencoded",
            "text/"
        ],
        StringComparer.OrdinalIgnoreCase).ToFrozenSet();

    /// <summary>
    ///     Paths to ignore for request logging.
    /// </summary>
    public static readonly FrozenSet<string> PathsToIgnore = new HashSet<string>(
        [
            "/openapi",
            "/above-board",
            "/favicon.ico",
            "/swagger"
        ],
        StringComparer.OrdinalIgnoreCase).ToFrozenSet();
}
