namespace SharedKernel.Logging.Middleware;

public static class LoggingOptions
{
   // keeps original defaults/behavior
   public const int RequestResponseBodyMaxBytes = 16 * 1024; // HttpLogHelper.MaxBodyBytes
   public const int RedactionMaxPropertyBytes = 2 * 1024; // RedactionHelper.MaxPropertyBytes
   public const long ResponseBufferLimitBytes = 10L * 1024 * 1024; // FileBufferingWriteStream limit

   // Sensitive words to redact in headers and JSON bodies
   public static readonly HashSet<string> SensitiveKeywords = new(StringComparer.OrdinalIgnoreCase)
   {
      "pwd",
      "pass",
      "secret",
      "token",
      "cookie",
      "auth",
      "pan",
      "cvv",
      "cvc",
      "cardholder",
      "bindingid",
      "ssn",
      "tin",
      "iban",
      "swift",
      "bankaccount",
      "notboundcard"
   };
   

   // was HttpLogHelper.TextLikeMedia
   public static readonly string[] TextLikeMediaPrefixes =
   [
      "application/json",
      "application/x-www-form-urlencoded",
      "text/"
   ];

   // was RequestLoggingMiddleware.PathsToIgnore
   public static readonly HashSet<string> PathsToIgnore = new(StringComparer.OrdinalIgnoreCase)
   {
      "/openapi",
      "/above-board"
   };
}