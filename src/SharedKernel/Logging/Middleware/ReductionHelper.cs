using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace SharedKernel.Logging.Middleware;

internal static class RedactionHelper
{
   public static Dictionary<string, string> RedactHeaders(IHeaderDictionary headers) =>
      headers.ToDictionary(
         h => h.Key,
         h => LoggingOptions.SensitiveKeywords.Any(k => h.Key.Contains(k, StringComparison.OrdinalIgnoreCase))
            ? "[REDACTED]"
            : h.Value.ToString());

   public static Dictionary<string, string> RedactHeaders(Dictionary<string, IEnumerable<string>> headers) =>
      headers.ToDictionary(
         kvp => kvp.Key,
         kvp => LoggingOptions.SensitiveKeywords.Any(k => kvp.Key.Contains(k, StringComparison.OrdinalIgnoreCase))
            ? "[REDACTED]"
            : string.Join(";", kvp.Value));

   public static object RedactBody(string? mediaType, string raw)
   {
      if (string.IsNullOrWhiteSpace(raw)) return string.Empty;

      if (IsJsonMediaType(mediaType))
      {
         try
         {
            var el = JsonSerializer.Deserialize<JsonElement>(raw);
            return RedactElement(el);
         }
         catch (JsonException)
         {
            return "[INVALID_JSON]";
         }
      }

      if (mediaType?.Equals("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase) == true)
      {
         var nvc = System.Web.HttpUtility.ParseQueryString(raw);
         var keys = nvc.AllKeys;

         var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
         foreach (var k in keys)
         {
            if (string.IsNullOrEmpty(k)) continue;
            var v = nvc[k] ?? string.Empty;
            dict[k] = LoggingOptions.SensitiveKeywords.Any(s =>
               k.Contains(s, StringComparison.OrdinalIgnoreCase) ||
               v.Contains(s, StringComparison.OrdinalIgnoreCase))
               ? "[REDACTED]"
               : v;
         }

         return dict;
      }

      var rawBytes = Encoding.UTF8.GetByteCount(raw);
      if (rawBytes <= LoggingOptions.RedactionMaxPropertyBytes)
      {
         return LoggingOptions.SensitiveKeywords.Any(s => raw.Contains(s, StringComparison.OrdinalIgnoreCase))
            ? "[REDACTED]"
            : raw;
      }

      return HttpLogHelper.BuildOmittedBodyMessage("exceeds-limit", rawBytes, mediaType, LoggingOptions.RedactionMaxPropertyBytes);
   }

   private static bool IsJsonMediaType(string? mediaType) =>
      !string.IsNullOrWhiteSpace(mediaType)
      && (mediaType.EndsWith("/json", StringComparison.OrdinalIgnoreCase)
          || mediaType.EndsWith("+json", StringComparison.OrdinalIgnoreCase));

   private static object RedactElement(JsonElement el) =>
      el.ValueKind switch
      {
         JsonValueKind.Object => el.EnumerateObject()
                                   .ToDictionary(
                                      p => p.Name,
                                      p => LoggingOptions.SensitiveKeywords.Any(k =>
                                         p.Name.Contains(k, StringComparison.OrdinalIgnoreCase))
                                         ? "[REDACTED]"
                                         : RedactElement(p.Value)),
         JsonValueKind.Array => el.EnumerateArray()
                                  .Select(RedactElement)
                                  .ToArray(),
         JsonValueKind.String => RedactString(el.GetString()!),
         _ => el.GetRawText()
      };

   private static string RedactString(string value)
   {
      var bytes = Encoding.UTF8.GetByteCount(value);
      if (bytes <= LoggingOptions.RedactionMaxPropertyBytes)
      {
         return LoggingOptions.SensitiveKeywords.Any(s => value.Contains(s, StringComparison.OrdinalIgnoreCase))
            ? "[REDACTED]"
            : value;
      }

      return HttpLogHelper.BuildOmittedBodyMessage("exceeds-limit", bytes, null, LoggingOptions.RedactionMaxPropertyBytes);
   }
}