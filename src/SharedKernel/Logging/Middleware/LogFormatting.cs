using System.Text.Encodings.Web;
using System.Text.Json;

namespace SharedKernel.Logging.Middleware;

internal static class LogFormatting
{
   private static readonly JsonSerializerOptions JsonOptions = new()
   {
      WriteIndented = false,
      PropertyNamingPolicy = null,
      // This allows non-ASCII characters (Armenian, Cyrillic, etc.) to pass through without escaping
      Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
   };

   public static object Omitted(string reason, long? lengthBytes, string? mediaType, int thresholdBytes)
   {
      int? sizeKb = lengthBytes.HasValue ? (int)Math.Round(lengthBytes.Value / 1024d) : null;
      return new Dictionary<string, object?>
      {
         ["omitted"] = true,
         ["reason"] = reason,
         ["sizeKb"] = sizeKb,
         ["thresholdKb"] = thresholdBytes / 1024,
         ["contentType"] = MediaTypeUtil.Normalize(mediaType)
      };
   }

   /// <summary>
   /// Converts the body object to a JSON string representation.
   /// This prevents Elasticsearch field explosion by storing bodies as strings instead of objects.
   /// </summary>
   public static string ToJsonString(object? body)
   {
      switch (body)
      {
         case null:
            return "{}";
         case string s:
            return s;
         default:
            try
            {
               return JsonSerializer.Serialize(body, JsonOptions);
            }
            catch
            {
               return "{}";
            }
      }
   }
}