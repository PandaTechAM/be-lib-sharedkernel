using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace SharedKernel.Logging.Helpers;

internal static class HttpLogHelper
{
   public static async Task<(string Headers, string Body)> CaptureAsync(Stream bodyStream,
      IHeaderDictionary headers,
      string? mediaType)
   {
      using var reader = new StreamReader(bodyStream, leaveOpen: true);
      var raw = await reader.ReadToEndAsync();
      bodyStream.Position = 0;
      var hdrs = RedactionHelper.RedactHeaders(headers);
      var body = RedactionHelper.RedactBody(mediaType, raw);
      return (JsonSerializer.Serialize(hdrs), JsonSerializer.Serialize(body));
   }

   public static async Task<(string Headers, string Body)> CaptureAsync(Dictionary<string, IEnumerable<string>> headers,
      Func<Task<string>> rawReader,
      string? mediaType)
   {
      var hdrs = RedactionHelper.RedactHeaders(headers);
      var raw = await rawReader();
      var body = RedactionHelper.RedactBody(mediaType, raw);
      return (JsonSerializer.Serialize(hdrs), JsonSerializer.Serialize(body));
   }

   public static Dictionary<string, IEnumerable<string>> CreateHeadersDictionary(HttpRequestMessage req)
   {
      var dict = new Dictionary<string, IEnumerable<string>>(StringComparer.OrdinalIgnoreCase);
      foreach (var h in req.Headers) dict[h.Key] = h.Value;
      if (req.Content?.Headers == null)
      {
         return dict;
      }

      {
         foreach (var h in req.Content.Headers)
            dict[h.Key] = h.Value;
      }
      return dict;
   }

   public static Dictionary<string, IEnumerable<string>> CreateHeadersDictionary(HttpResponseMessage res)
   {
      var dict = new Dictionary<string, IEnumerable<string>>(StringComparer.OrdinalIgnoreCase);
      foreach (var h in res.Headers) dict[h.Key] = h.Value;
      {
         foreach (var h in res.Content.Headers)
            dict[h.Key] = h.Value;
      }
      return dict;
   }
}