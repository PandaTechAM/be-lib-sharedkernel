using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace SharedKernel.Logging.Middleware;

internal sealed partial class OutboundLoggingHandler(ILogger<OutboundLoggingHandler> logger) : DelegatingHandler
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

      LogHttpOut(
         request.Method.Method,
         request.RequestUri?.GetLeftPart(UriPartial.Path) ?? "",
         (int)response.StatusCode,
         elapsedMs,
         "HttpOut",
         string.IsNullOrEmpty(request.RequestUri?.Query) ? null : request.RequestUri!.Query,
         LogFormatting.ToJsonString(reqHeaders),
         LogFormatting.ToJsonString(reqBody),
         LogFormatting.ToJsonString(resHeaders),
         LogFormatting.ToJsonString(resBody));

      return response;
   }

   private static async Task<(object Headers, object Body)> CaptureRequestAsync(HttpRequestMessage request,
      CancellationToken ct)
   {
      var headerDict = HttpLogHelper.CreateHeadersDictionary(request);
      var redactedHeaders = RedactionHelper.RedactHeaders(headerDict);

      if (request.Content is null)
      {
         return (redactedHeaders, new Dictionary<string, object?>());
      }

      var mediaType = request.Content.Headers.ContentType?.MediaType;
      var contentLength = request.Content.Headers.ContentLength;

      switch (request.Content)
      {
         // MULTIPART: Never enumerate or read — it corrupts internal state.
         case MultipartFormDataContent:
            return (redactedHeaders, new Dictionary<string, object?>
            {
               ["_type"] = "multipart/form-data",
               ["_contentLength"] = contentLength,
               ["_note"] = "multipart body not captured to preserve request integrity"
            });
         // STREAM CONTENT: Not safe to read — would consume the stream.
         case StreamContent:
            return (redactedHeaders, new Dictionary<string, object?>
            {
               ["_type"] = mediaType,
               ["_contentLength"] = contentLength,
               ["_note"] = "stream body not captured to preserve request integrity"
            });
      }

      if (!MediaTypeUtil.IsTextLike(mediaType))
      {
         return (redactedHeaders,
            LogFormatting.Omitted("non-text", contentLength, mediaType, LoggingOptions.RequestResponseBodyMaxBytes));
      }

      if (contentLength is > LoggingOptions.RequestResponseBodyMaxBytes)
      {
         return (redactedHeaders,
            LogFormatting.Omitted("exceeds-limit",
               contentLength,
               mediaType,
               LoggingOptions.RequestResponseBodyMaxBytes));
      }

      // SAFE TO READ: ByteArrayContent, StringContent, FormUrlEncodedContent, ReadOnlyMemoryContent.
      // All backed by byte arrays and support multiple reads.
      var raw = await request.Content.ReadAsStringAsync(ct);

      // Double-check size after reading (in case Content-Length was absent).
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

      if (contentLength == 0)
      {
         return (redactedHeaders, new Dictionary<string, object?>());
      }

      if (!MediaTypeUtil.IsTextLike(mediaType))
      {
         return (redactedHeaders,
            LogFormatting.Omitted("non-text", contentLength, mediaType, LoggingOptions.RequestResponseBodyMaxBytes));
      }

      if (contentLength > LoggingOptions.RequestResponseBodyMaxBytes)
      {
         return (redactedHeaders,
            LogFormatting.Omitted("exceeds-limit",
               contentLength,
               mediaType,
               LoggingOptions.RequestResponseBodyMaxBytes));
      }

      // Response bodies are always safe to read — already fully received (includes chunked).
      return await HttpLogHelper.CaptureAsync(
         headerDict,
         () => response.Content.ReadAsStringAsync(ct),
         mediaType,
         ct);
   }

   // All named placeholders become structured properties in Serilog / Elasticsearch.
   // Eliminates the BeginScope dictionary allocation and the LogInformation args-array allocation.
   [LoggerMessage(Level = LogLevel.Information,
      Message = "[HTTP OUT] {Method} {HostPath} -> {StatusCode} in {ElapsedMs}ms | " +
                "{Kind} q={Query} rqH={RequestHeaders} rqB={RequestBody} rsH={ResponseHeaders} rsB={ResponseBody}")]
   private partial void LogHttpOut(string method,
      string hostPath,
      int statusCode,
      double elapsedMs,
      string kind,
      string? query,
      string requestHeaders,
      string requestBody,
      string responseHeaders,
      string responseBody);
}