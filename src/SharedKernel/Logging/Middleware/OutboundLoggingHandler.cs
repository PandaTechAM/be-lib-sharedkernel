using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace SharedKernel.Logging.Middleware;

internal sealed class OutboundLoggingHandler(ILogger<OutboundLoggingHandler> logger) : DelegatingHandler
{
   protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
      CancellationToken cancellationToken)
   {
      var timestamp = Stopwatch.GetTimestamp();

      var (reqHeaders, reqBody) = await CaptureRequestAsync(request, cancellationToken);

      var response = await base.SendAsync(request, cancellationToken);

      var elapsedMs = Stopwatch.GetElapsedTime(timestamp)
                               .TotalMilliseconds;

      var (resHeaders, resBody) = await CaptureResponseAsync(response, cancellationToken);

      LogHttpOut(request, response, reqHeaders, reqBody, resHeaders, resBody, elapsedMs);

      return response;
   }

   private static async Task<(object Headers, object Body)> CaptureRequestAsync(HttpRequestMessage request,
      CancellationToken ct)
   {
      var headerDict = HttpLogHelper.CreateHeadersDictionary(request);
      var redactedHeaders = RedactionHelper.RedactHeaders(headerDict);

      if (request.Content is null)
         return (redactedHeaders, new Dictionary<string, object?>());

      var mediaType = request.Content.Headers.ContentType?.MediaType;
      var contentLength = request.Content.Headers.ContentLength;

      // MULTIPART: Never enumerate or read - it corrupts internal state
      if (request.Content is MultipartFormDataContent)
      {
         return (redactedHeaders, new Dictionary<string, object?>
         {
            ["_type"] = "multipart/form-data",
            ["_contentLength"] = contentLength,
            ["_note"] = "multipart body not captured to preserve request integrity"
         });
      }

      // STREAM CONTENT: Not safe to read - would consume the stream
      if (request.Content is StreamContent)
      {
         return (redactedHeaders, new Dictionary<string, object?>
         {
            ["_type"] = mediaType,
            ["_contentLength"] = contentLength,
            ["_note"] = "stream body not captured to preserve request integrity"
         });
      }

      // NON-TEXT: Just log metadata
      if (!MediaTypeUtil.IsTextLike(mediaType))
      {
         return (redactedHeaders,
            LogFormatting.Omitted("non-text", contentLength, mediaType, LoggingOptions.RequestResponseBodyMaxBytes));
      }

      // TOO LARGE: Don't read
      if (contentLength is > LoggingOptions.RequestResponseBodyMaxBytes)
      {
         return (redactedHeaders,
            LogFormatting.Omitted("exceeds-limit", contentLength, mediaType, LoggingOptions.RequestResponseBodyMaxBytes));
      }

      // SAFE TO READ: ByteArrayContent, StringContent, FormUrlEncodedContent, ReadOnlyMemoryContent
      // These are all backed by byte arrays and support multiple reads
      var raw = await request.Content.ReadAsStringAsync(ct);
      
      // Double-check size after reading (in case contentLength was null)
      if (raw.Length > LoggingOptions.RequestResponseBodyMaxBytes)
      {
         return (redactedHeaders,
            LogFormatting.Omitted("exceeds-limit", raw.Length, mediaType, LoggingOptions.RequestResponseBodyMaxBytes));
      }
      
      var body = RedactionHelper.RedactBody(mediaType, raw);
      return (redactedHeaders, body);
   }

   private static async Task<(object Headers, object Body)> CaptureResponseAsync(HttpResponseMessage response,
      CancellationToken ct)
   {
      var headerDict = HttpLogHelper.CreateHeadersDictionary(response);
      var redactedHeaders = RedactionHelper.RedactHeaders(headerDict);

      var mediaType = response.Content.Headers.ContentType?.MediaType;
      var contentLength = response.Content.Headers.ContentLength;

      // Empty response (explicit Content-Length: 0)
      if (contentLength == 0)
         return (redactedHeaders, new Dictionary<string, object?>());

      // Non-text content
      if (!MediaTypeUtil.IsTextLike(mediaType))
      {
         return (redactedHeaders,
            LogFormatting.Omitted("non-text", contentLength, mediaType, LoggingOptions.RequestResponseBodyMaxBytes));
      }

      // Known to be too large
      if (contentLength > LoggingOptions.RequestResponseBodyMaxBytes)
      {
         return (redactedHeaders,
            LogFormatting.Omitted("exceeds-limit", contentLength, mediaType, LoggingOptions.RequestResponseBodyMaxBytes));
      }

      // SAFE TO READ: Response bodies are always safe - they've already been received
      // This includes chunked responses (no Content-Length header)
      return await HttpLogHelper.CaptureAsync(
         headerDict,
         () => response.Content.ReadAsStringAsync(ct),
         mediaType,
         ct);
   }

   private void LogHttpOut(HttpRequestMessage request,
      HttpResponseMessage response,
      object reqHeaders,
      object reqBody,
      object resHeaders,
      object resBody,
      double elapsedMs)
   {
      var hostPath = request.RequestUri?.GetLeftPart(UriPartial.Path) ?? "";

      var scope = new Dictionary<string, object?>
      {
         ["RequestHeaders"] = LogFormatting.ToJsonString(reqHeaders),
         ["RequestBody"] = LogFormatting.ToJsonString(reqBody),
         ["ResponseHeaders"] = LogFormatting.ToJsonString(resHeaders),
         ["ResponseBody"] = LogFormatting.ToJsonString(resBody),
         ["ElapsedMs"] = elapsedMs,
         ["Kind"] = "HttpOut"
      };

      if (!string.IsNullOrEmpty(request.RequestUri?.Query))
         scope["Query"] = request.RequestUri!.Query;

      using (logger.BeginScope(scope))
      {
         logger.LogInformation(
            "[HTTP OUT] {Method} {HostPath} -> {StatusCode} in {ElapsedMilliseconds}ms",
            request.Method,
            hostPath,
            (int)response.StatusCode,
            elapsedMs);
      }
   }
}