using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;

namespace SharedKernel.Logging.Middleware;

internal static class RedactionHelper
{
   // ------- Headers -------

   public static Dictionary<string, string> RedactHeaders(IHeaderDictionary headers)
   {
      return headers.ToDictionary(
         h => h.Key,
         h => LoggingOptions.SensitiveKeywords.Any(k => h.Key.Contains(k, StringComparison.OrdinalIgnoreCase))
            ? "[REDACTED]"
            : h.Value.ToString());
   }

   public static Dictionary<string, string> RedactHeaders(Dictionary<string, IEnumerable<string>> headers)
   {
      return headers.ToDictionary(
         kvp => kvp.Key,
         kvp => LoggingOptions.SensitiveKeywords.Any(k => kvp.Key.Contains(k, StringComparison.OrdinalIgnoreCase))
            ? "[REDACTED]"
            : string.Join(";", kvp.Value));
   }

   // ------- Bodies (JSON, x-www-form-urlencoded, text fallback) -------

   public static object RedactBody(string? contentType, string raw)
   {
      if (string.IsNullOrWhiteSpace(raw))
      {
         return new Dictionary<string, object?>();
      }

      // JSON (including +json)
      if (MediaTypeUtil.IsJson(contentType))
      {
         try
         {
            var el = JsonSerializer.Deserialize<JsonElement>(raw);
            return RedactElement(el);
         }
         catch (JsonException)
         {
            return new Dictionary<string, object?>
            {
               ["invalidJson"] = true
            };
         }
      }

      // application/x-www-form-urlencoded
      if (string.Equals(MediaTypeUtil.Normalize(contentType),
             "application/x-www-form-urlencoded",
             StringComparison.OrdinalIgnoreCase))
      {
         var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
         var parsed = QueryHelpers.ParseQuery("?" + raw);
         foreach (var kvp in parsed)
         {
            var k = kvp.Key;

            if (string.IsNullOrEmpty(k))
            {
               continue;
            }

            var joined = string.Join(";", kvp.Value.ToArray());
            dict[k] = RedactFormValue(k, joined);
         }

         return dict;
      }

      var rawBytes = Encoding.UTF8.GetByteCount(raw);

      if (rawBytes > LoggingOptions.RedactionMaxPropertyBytes)
      {
         return new Dictionary<string, object?>
         {
            ["text"] = $"[OMITTED: exceeds-limit ~{rawBytes / 1024}KB]"
         };
      }

      var val = LoggingOptions.SensitiveKeywords.Any(s => raw.Contains(s, StringComparison.OrdinalIgnoreCase))
         ? "[REDACTED]"
         : raw;

      return new Dictionary<string, object?>
      {
         ["text"] = val
      };
   }

   // ------- Forms (fields only; add file placeholders) -------

   public static Dictionary<string, string> RedactFormFields(IFormCollection form)
   {
      var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

      // text fields
      foreach (var kvp in form)
      {
         var raw = string.Join(";", kvp.Value.ToArray());
         fields[kvp.Key] = RedactFormValue(kvp.Key, raw);
      }

      // file placeholders
      if (form.Files.Count <= 0)
      {
         return fields;
      }

      var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
      var sizes = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);

      foreach (var f in form.Files)
      {
         var key = f.Name;
         counts.TryGetValue(key, out var c);
         counts[key] = c + 1;

         sizes.TryGetValue(key, out var b);
         sizes[key] = b + f.Length;
      }

      foreach (var key in counts.Keys)
      {
         var count = counts[key];
         var sizeKb = (int)Math.Round(sizes[key] / 1024d);

         var place = count == 1
            ? $"[OMITTED: file {sizeKb}KB]"
            : $"[OMITTED: {count} files {sizeKb}KB]";

         if (fields.TryGetValue(key, out var existing) && !string.IsNullOrWhiteSpace(existing))
         {
            fields[key] = $"{existing}; {place}";
         }
         else
         {
            fields[key] = place;
         }
      }

      return fields;
   }

   // ------- Helpers -------

   private static object RedactElement(JsonElement el)
   {
      return el.ValueKind switch
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
         JsonValueKind.Number => el.TryGetInt64(out var i) ? i :
            el.TryGetDouble(out var d) ? d :
            decimal.TryParse(el.GetRawText(), NumberStyles.Any, CultureInfo.InvariantCulture, out var m) ? m :
            el.GetRawText(),
         JsonValueKind.True => true,
         JsonValueKind.False => false,
         JsonValueKind.Null => null!,
         _ => el.GetRawText()
      };
   }

   private static string RedactString(string value)
   {
      var bytes = Encoding.UTF8.GetByteCount(value);
      if (bytes <= LoggingOptions.RedactionMaxPropertyBytes)
      {
         return LoggingOptions.SensitiveKeywords.Any(s => value.Contains(s, StringComparison.OrdinalIgnoreCase))
            ? "[REDACTED]"
            : value;
      }

      return $"[OMITTED: exceeds-limit ~{bytes / 1024}KB]";
   }

   internal static string RedactFormValue(string key, string value)
   {
      if (LoggingOptions.SensitiveKeywords.Any(k =>
             key.Contains(k, StringComparison.OrdinalIgnoreCase) ||
             value.Contains(k, StringComparison.OrdinalIgnoreCase)))
      {
         return "[REDACTED]";
      }

      var bytes = Encoding.UTF8.GetByteCount(value);

      return bytes > LoggingOptions.RedactionMaxPropertyBytes ? $"[OMITTED: exceeds-limit ~{bytes / 1024}KB]" : value;
   }
}