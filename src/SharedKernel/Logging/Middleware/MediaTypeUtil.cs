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
         return mt.MediaType.Value;
      }
      catch
      {
         var semiIndex = contentType.IndexOf(';');
         return (semiIndex >= 0 ? contentType[..semiIndex] : contentType).Trim();
      }
   }

   public static bool IsJson(string? contentType)
   {
      var mt = Normalize(contentType);
      return !string.IsNullOrWhiteSpace(mt) &&
             (mt.EndsWith("/json", StringComparison.OrdinalIgnoreCase) ||
              mt.EndsWith("+json", StringComparison.OrdinalIgnoreCase));
   }

   public static bool IsFormUrlEncoded(string? contentType) =>
      string.Equals(Normalize(contentType), "application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase);

   public static bool IsMultipartForm(string? contentType) =>
      Normalize(contentType)?.StartsWith("multipart/form-data", StringComparison.OrdinalIgnoreCase) == true;

   // Normalizes once instead of delegating to IsFormUrlEncoded + IsMultipartForm (which each normalize separately).
   public static bool IsFormLike(string? contentType)
   {
      var mt = Normalize(contentType);
      return string.Equals(mt, "application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase)
          || mt?.StartsWith("multipart/form-data", StringComparison.OrdinalIgnoreCase) == true;
   }

   public static bool IsTextLike(string? contentType)
   {
      var mt = Normalize(contentType);
      if (string.IsNullOrWhiteSpace(mt))
      {
         return false;
      }

      // Catches vendor types like application/vnd.api+json
      if (mt.EndsWith("+json", StringComparison.OrdinalIgnoreCase))
      {
         return true;
      }

      foreach (var prefix in LoggingOptions.TextLikeMediaPrefixes)
      {
         if (mt.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
         {
            return true;
         }
      }

      return false;
   }
}