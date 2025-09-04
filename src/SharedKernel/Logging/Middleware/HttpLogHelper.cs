using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace SharedKernel.Logging.Middleware;

internal static class HttpLogHelper
{
   public static async Task<(object Headers, object Body)> CaptureAsync(Stream bodyStream,
      IHeaderDictionary headers,
      string? contentType)
   {
      var redactedHeaders = RedactionHelper.RedactHeaders(headers);

      var textLike = MediaTypeUtil.IsTextLike(contentType);
      var hasContentLength = headers.ContainsKey(HeaderNames.ContentLength);
      var len = GetContentLengthOrNull(headers);
      var hasChunked = headers.TryGetValue(HeaderNames.TransferEncoding, out _);

      if ((hasContentLength && len == 0) ||
          (!hasContentLength && !hasChunked && string.IsNullOrWhiteSpace(contentType)))
      {
         return (redactedHeaders, new Dictionary<string, object?>());
      }

      if (!textLike)
      {
         return (redactedHeaders, LogFormatting.Omitted(
            reason: "non-text",
            lengthBytes: len,
            mediaType: MediaTypeUtil.Normalize(contentType),
            thresholdBytes: LoggingOptions.RequestResponseBodyMaxBytes));
      }

      var (raw, truncated) = await ReadLimitedAsync(bodyStream, LoggingOptions.RequestResponseBodyMaxBytes);
      if (truncated)
      {
         return (redactedHeaders, LogFormatting.Omitted(
            reason: "exceeds-limit",
            lengthBytes: LoggingOptions.RequestResponseBodyMaxBytes,
            mediaType: MediaTypeUtil.Normalize(contentType),
            thresholdBytes: LoggingOptions.RequestResponseBodyMaxBytes));
      }

      var body = RedactionHelper.RedactBody(contentType, raw);
      return (redactedHeaders, body);
   }

   public static async Task<(object Headers, object Body)> CaptureAsync(Dictionary<string, IEnumerable<string>> headers,
      Func<Task<string>> rawReader,
      string? contentType)
   {
      var redactedHeaders = RedactionHelper.RedactHeaders(headers);

      if (!MediaTypeUtil.IsTextLike(contentType))
      {
         return (redactedHeaders, new Dictionary<string, object?>());
      }

      var raw = await rawReader();

      if (Utf8ByteCount(raw) > LoggingOptions.RequestResponseBodyMaxBytes)
      {
         return (redactedHeaders, LogFormatting.Omitted(
            reason: "exceeds-limit",
            lengthBytes: Utf8ByteCount(raw),
            mediaType: MediaTypeUtil.Normalize(contentType),
            thresholdBytes: LoggingOptions.RequestResponseBodyMaxBytes));
      }

      var body = RedactionHelper.RedactBody(contentType, raw);
      return (redactedHeaders, body);
   }

   public static Dictionary<string, IEnumerable<string>> CreateHeadersDictionary(HttpRequestMessage req)
   {
      var dict = new Dictionary<string, IEnumerable<string>>(StringComparer.OrdinalIgnoreCase);
      foreach (var h in req.Headers)
      {
         dict[h.Key] = h.Value;
      }

      var contentHeaders = req.Content?.Headers;

      if (contentHeaders != null)
      {
         foreach (var h in contentHeaders)
         {
            dict[h.Key] = h.Value;
         }
      }

      return dict;
   }

   public static Dictionary<string, IEnumerable<string>> CreateHeadersDictionary(HttpResponseMessage res)
   {
      var dict = new Dictionary<string, IEnumerable<string>>(StringComparer.OrdinalIgnoreCase);
      foreach (var h in res.Headers) dict[h.Key] = h.Value;

      var ch = res.Content?.Headers;
      if (ch != null)
      {
         foreach (var h in ch)
         {
            dict[h.Key] = h.Value;
         }
      }

      return dict;
   }

   internal static bool IsTextLike(string? mediaType) => MediaTypeUtil.IsTextLike(mediaType);

   private static long? GetContentLengthOrNull(IHeaderDictionary headers)
   {
      if (headers.TryGetValue("Content-Length", out var clVal) &&
          long.TryParse(clVal.ToString(), out var cl))
      {
         return cl;
      }

      return null;
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