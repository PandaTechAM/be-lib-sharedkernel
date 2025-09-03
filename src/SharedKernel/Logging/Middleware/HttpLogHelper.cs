using System.Text;
using Microsoft.AspNetCore.Http;

namespace SharedKernel.Logging.Middleware;

internal static class HttpLogHelper
{
   // defaults; can be overridden via Configure(options)

   public static async Task<(string Headers, string Body)> CaptureAsync(Stream bodyStream,
      IHeaderDictionary headers,
      string? mediaType)
   {
      var hdrs = RedactionHelper.RedactHeaders(headers);

      if (!IsTextLike(mediaType))
      {
         long? len = null;
         if (headers.TryGetValue("Content-Length", out var clVal) &&
             long.TryParse(clVal.ToString(), out var cl))
            len = cl;

         return (LogFormatting.Json(hdrs),
            LogFormatting.Json(BuildOmittedBodyMessage("non-text",
               len,
               mediaType,
               LoggingOptions.RequestResponseBodyMaxBytes)));
      }

      var (raw, truncated) = await ReadLimitedAsync(bodyStream, LoggingOptions.RequestResponseBodyMaxBytes);

      if (truncated)
         return (LogFormatting.Json(hdrs),
            LogFormatting.Json($"[OMITTED: body exceeds {LoggingOptions.RequestResponseBodyMaxBytes / 1024}KB]"));

      var body = RedactionHelper.RedactBody(mediaType, raw);
      return (LogFormatting.Json(hdrs), LogFormatting.Json(body));
   }

   public static async Task<(string Headers, string Body)> CaptureAsync(Dictionary<string, IEnumerable<string>> headers,
      Func<Task<string>> rawReader,
      string? mediaType)
   {
      var hdrs = RedactionHelper.RedactHeaders(headers);

      if (!IsTextLike(mediaType))
         return (LogFormatting.Json(hdrs), LogFormatting.Json(string.Empty));

      var raw = await rawReader();
      if (Utf8ByteCount(raw) > LoggingOptions.RequestResponseBodyMaxBytes)
      {
         return (LogFormatting.Json(hdrs),
            LogFormatting.Json($"[OMITTED: body exceeds {LoggingOptions.RequestResponseBodyMaxBytes / 1024}KB]"));
      }

      var body = RedactionHelper.RedactBody(mediaType, raw);
      return (LogFormatting.Json(hdrs), LogFormatting.Json(body));
   }

   public static Dictionary<string, IEnumerable<string>> CreateHeadersDictionary(HttpRequestMessage req)
   {
      var dict = new Dictionary<string, IEnumerable<string>>(StringComparer.OrdinalIgnoreCase);
      foreach (var h in req.Headers) dict[h.Key] = h.Value;

      var contentHeaders = req.Content?.Headers;
      if (contentHeaders != null)
         foreach (var h in contentHeaders)
            dict[h.Key] = h.Value;

      return dict;
   }

   public static Dictionary<string, IEnumerable<string>> CreateHeadersDictionary(HttpResponseMessage res)
   {
      var dict = new Dictionary<string, IEnumerable<string>>(StringComparer.OrdinalIgnoreCase);
      foreach (var h in res.Headers) dict[h.Key] = h.Value;
      foreach (var h in res.Content.Headers) dict[h.Key] = h.Value;
      return dict;
   }

   internal static string BuildOmittedBodyMessage(string reason,
      long? lengthBytes,
      string? mediaType,
      int thresholdBytes) =>
      LogFormatting.Omitted(reason, lengthBytes, mediaType, thresholdBytes);

   internal static bool IsTextLike(string? mediaType)
   {
      if (string.IsNullOrWhiteSpace(mediaType)) return false;
      return LoggingOptions.TextLikeMediaPrefixes.Any(m => mediaType.StartsWith(m, StringComparison.OrdinalIgnoreCase))
             || mediaType.EndsWith("+json", StringComparison.OrdinalIgnoreCase);
   }

   private static async Task<(string text, bool truncated)> ReadLimitedAsync(Stream s, int maxBytes)
   {
      s.Seek(0, SeekOrigin.Begin);

      using var ms = new MemoryStream(capacity: maxBytes);
      var buf = new byte[Math.Min(8192, maxBytes)];
      var total = 0;

      while (total < maxBytes)
      {
         var toRead = Math.Min(buf.Length, maxBytes - total);
         var read = await s.ReadAsync(buf.AsMemory(0, toRead));
         if (read == 0) break;
         await ms.WriteAsync(buf.AsMemory(0, read));
         total += read;
      }

      var truncated = false;
      if (total == maxBytes)
      {
         var probe = new byte[1];
         var read = await s.ReadAsync(probe.AsMemory(0, 1));
         if (read > 0)
         {
            truncated = true;
            if (s.CanSeek) s.Seek(-read, SeekOrigin.Current);
         }
      }

      s.Seek(0, SeekOrigin.Begin);
      return (Encoding.UTF8.GetString(ms.ToArray()), truncated);
   }

   private static int Utf8ByteCount(string s) => Encoding.UTF8.GetByteCount(s);
}