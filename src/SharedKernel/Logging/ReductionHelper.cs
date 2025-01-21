using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace SharedKernel.Logging;

internal static class RedactionHelper
{
   private const int MaxPropertyLength = 1024 * 5; // 5KB

   private static readonly HashSet<string> SensitiveKeywords = new(StringComparer.OrdinalIgnoreCase)
   {
      "pwd",
      "pass",
      "secret",
      "token",
      "cookie",
      "auth"
   };

   // -------------------------------------------------
   // Public Redaction APIs for Headers
   // -------------------------------------------------

   // For ASP.NET Core request/response headers:
   public static Dictionary<string, string> RedactHeaders(IHeaderDictionary headers)
   {
      var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
      foreach (var header in headers)
      {
         var key = header.Key;
         var value = SensitiveKeywords.Any(k => key.Contains(k, StringComparison.OrdinalIgnoreCase))
            ? "[REDACTED]"
            : header.Value.ToString();

         result[key] = value;
      }

      return result;
   }

   // For outbound HTTP requests/responses with a dictionary of values:
   public static Dictionary<string, string> RedactHeaders(Dictionary<string, IEnumerable<string>> headers)
   {
      var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
      foreach (var kvp in headers)
      {
         var key = kvp.Key;
         var joinedValue = string.Join(";", kvp.Value);

         result[key] = SensitiveKeywords.Any(k => key.Contains(k, StringComparison.OrdinalIgnoreCase))
            ? "[REDACTED]"
            : joinedValue;
      }

      return result;
   }

   // -------------------------------------------------
   // Public Redaction APIs for Bodies
   // -------------------------------------------------

   public static object ParseAndRedactJson(string body)
   {
      if (string.IsNullOrWhiteSpace(body))
         return string.Empty;

      try
      {
         var jsonElement = JsonSerializer.Deserialize<JsonElement>(body);
         return RedactSensitiveData(jsonElement);
      }
      catch (JsonException)
      {
         // If invalid JSON, do naive string-based redaction
         return RedactSensitiveString(body);
      }
   }

   // -------------------------------------------------
   // Internal Implementation Details
   // -------------------------------------------------

   private static object RedactSensitiveData(JsonElement element)
   {
      return element.ValueKind switch
      {
         JsonValueKind.Object => RedactObject(element),
         JsonValueKind.Array => element.EnumerateArray()
                                       .Select(RedactSensitiveData)
                                       .ToArray(),
         JsonValueKind.String => RedactSensitiveString(element.GetString()),
         _ => element.GetRawText()
      };
   }

   private static Dictionary<string, object> RedactObject(JsonElement element)
   {
      var masked = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

      foreach (var property in element.EnumerateObject())
      {
         var propName = property.Name;
         var propVal = property.Value;

         if (SensitiveKeywords.Any(k => propName.Contains(k, StringComparison.OrdinalIgnoreCase)))
         {
            masked[propName] = "[REDACTED]";
            continue;
         }

         switch (propVal.ValueKind)
         {
            // Omit large arrays
            case JsonValueKind.Array when propVal.GetRawText()
                                                 .Length > MaxPropertyLength:
               masked[propName] = "[OMITTED_DUE_TO_SIZE]";
               break;
            case JsonValueKind.String:
            {
               var s = propVal.GetString() ?? string.Empty;
               masked[propName] = s.Length > MaxPropertyLength
                  ? "[OMITTED_DUE_TO_SIZE]"
                  : RedactSensitiveString(s);
               break;
            }
            default:
               masked[propName] = RedactSensitiveData(propVal);
               break;
         }
      }

      return masked;
   }

   private static string RedactSensitiveString(string? value)
   {
      if (string.IsNullOrWhiteSpace(value))
         return string.Empty;

      return SensitiveKeywords.Any(k => value.Contains(k, StringComparison.OrdinalIgnoreCase))
         ? "[REDACTED]"
         : value;
   }
}