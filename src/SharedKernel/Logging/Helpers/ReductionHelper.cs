﻿using System.Text.Json;
using System.Web;
using Microsoft.AspNetCore.Http;

namespace SharedKernel.Logging.Helpers;

internal static class RedactionHelper
{
   private const int MaxPropertyLength = 1024 * 5; // 5 KB

   private static readonly HashSet<string> SensitiveKeywords = new(StringComparer.OrdinalIgnoreCase)
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

   public static Dictionary<string, string> RedactHeaders(IHeaderDictionary headers) =>
      headers.ToDictionary(
         h => h.Key,
         h => SensitiveKeywords.Any(k => h.Key.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0)
            ? "[REDACTED]"
            : h.Value.ToString()!);

   public static Dictionary<string, string> RedactHeaders(Dictionary<string, IEnumerable<string>> headers) =>
      headers.ToDictionary(
         kvp => kvp.Key,
         kvp => SensitiveKeywords.Any(k => kvp.Key.Contains(k, StringComparison.OrdinalIgnoreCase))
            ? "[REDACTED]"
            : string.Join(";", kvp.Value));

   public static object RedactBody(string? mediaType, string raw)
   {
      if (string.IsNullOrWhiteSpace(raw))
      {
         return string.Empty;
      }

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
         var nvc = HttpUtility.ParseQueryString(raw);
         var keys = nvc.AllKeys;

         var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
         foreach (var k in keys)
         {
            if (string.IsNullOrEmpty(k))
               continue;

            var v = nvc[k] ?? string.Empty;
            dict[k] = SensitiveKeywords.Any(s =>
               k.Contains(s, StringComparison.OrdinalIgnoreCase) ||
               v.Contains(s, StringComparison.OrdinalIgnoreCase))
               ? "[REDACTED]"
               : v;
         }

         return dict;
      }

      if (raw.Length <= MaxPropertyLength)
      {
         return SensitiveKeywords.Any(s => raw.Contains(s, StringComparison.OrdinalIgnoreCase))
            ? "[REDACTED]"
            : raw;
      }

      var maxKb = MaxPropertyLength / 1024;
      var actualKb = raw.Length / 1024;
      return $"[OMITTED: max {maxKb}KB, actual {actualKb}KB]";
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
                                      p => SensitiveKeywords.Any(k =>
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
      if (value.Length <= MaxPropertyLength)
      {
         return SensitiveKeywords.Any(s => value.Contains(s, StringComparison.OrdinalIgnoreCase))
            ? "[REDACTED]"
            : value;
      }

      const int maxKb = MaxPropertyLength / 1024;
      var actualKb = value.Length / 1024;
      return $"[OMITTED: max {maxKb}KB, actual {actualKb}KB]";
   }
}