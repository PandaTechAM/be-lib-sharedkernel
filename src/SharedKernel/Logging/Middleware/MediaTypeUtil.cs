using Microsoft.Net.Http.Headers;

namespace SharedKernel.Logging.Middleware;

internal static class MediaTypeUtil
{
   public static string? Normalize(string? contentType)
   {
      if (string.IsNullOrWhiteSpace(contentType))
      {
         return null;
      }

      try
      {
         var mt = MediaTypeHeaderValue.Parse(contentType);
         return mt.MediaType.Value; // e.g. "application/json"
      }
      catch
      {
         var semi = contentType.IndexOf(';');
         return (semi >= 0 ? contentType[..semi] : contentType).Trim();
      }
   }

   public static bool IsJson(string? contentType)
   {
      var mt = Normalize(contentType);
      return !string.IsNullOrWhiteSpace(mt) &&
             (mt.EndsWith("/json", StringComparison.OrdinalIgnoreCase) ||
              mt.EndsWith("+json", StringComparison.OrdinalIgnoreCase));
   }

   public static bool IsTextLike(string? contentType)
   {
      var mt = Normalize(contentType);
      if (string.IsNullOrWhiteSpace(mt))
      {
         return false;
      }

      return LoggingOptions.TextLikeMediaPrefixes.Any(p => mt.StartsWith(p, StringComparison.OrdinalIgnoreCase)) ||
             mt.EndsWith("+json", StringComparison.OrdinalIgnoreCase);
   }
}