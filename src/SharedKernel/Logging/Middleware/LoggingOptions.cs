using System.Collections.Frozen;

namespace SharedKernel.Logging.Middleware;

public static class LoggingOptions
{
   public const int RequestResponseBodyMaxBytes = 16 * 1024;
   public const int RedactionMaxPropertyBytes = 2 * 1024;

   /// <summary>
   /// Sensitive keywords to redact in headers and JSON bodies.
   /// Uses FrozenSet for optimized lookups on static data.
   /// </summary>
   public static readonly FrozenSet<string> SensitiveKeywords = new[]
   {
      "pwd", "pass", "secret", "token", "cookie", "auth",
      "pan", "cvv", "cvc", "cardholder", "bindingid",
      "ssn", "tin", "iban", "swift", "bankaccount", "notboundcard"
   }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

   /// <summary>
   /// Media type prefixes considered text-like for logging purposes.
   /// </summary>
   public static readonly FrozenSet<string> TextLikeMediaPrefixes = new[]
   {
      "application/json",
      "application/x-www-form-urlencoded",
      "text/"
   }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

   /// <summary>
   /// Paths to ignore for request logging.
   /// </summary>
   public static readonly FrozenSet<string> PathsToIgnore = new[]
   {
      "/openapi",
      "/above-board",
      "/favicon.ico",
      "/swagger"
   }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
}