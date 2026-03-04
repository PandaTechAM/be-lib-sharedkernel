using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace SharedKernel.Logging.Middleware;

internal sealed partial class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
{
   public async Task InvokeAsync(HttpContext context)
   {
      if (HttpMethods.IsOptions(context.Request.Method) || ShouldIgnorePath(context.Request.Path))
      {
         await next(context);
         return;
      }

      var (reqHeaders, reqBody) = await CaptureRequestAsync(context);

      var originalBody = context.Response.Body;
      await using var tee = new CappedResponseBodyStream(originalBody, LoggingOptions.RequestResponseBodyMaxBytes);
      context.Response.Body = tee;

      var timestamp = Stopwatch.GetTimestamp();

      try
      {
         await next(context);
      }
      finally
      {
         var elapsedMs = Stopwatch.GetElapsedTime(timestamp).TotalMilliseconds;
         var (resHeaders, resBody) = await CaptureResponseAsync(context, tee);

         context.Response.Body = originalBody;

         LogHttpIn(
            context.Request.Method,
            context.Request.Path.Value,
            context.Response.StatusCode,
            elapsedMs,
            "HttpIn",
            context.Request.QueryString.HasValue ? context.Request.QueryString.Value : null,
            LogFormatting.ToJsonString(reqHeaders),
            LogFormatting.ToJsonString(reqBody),
            LogFormatting.ToJsonString(resHeaders),
            LogFormatting.ToJsonString(resBody));
      }
   }

   private static bool ShouldIgnorePath(PathString path) =>
      path.HasValue && LoggingOptions.PathsToIgnore.Any(p => path.StartsWithSegments(p));

   private static async Task<(object Headers, object Body)> CaptureRequestAsync(HttpContext context,
      CancellationToken ct = default)
   {
      var request = context.Request;
      var normalizedContentType = MediaTypeUtil.Normalize(request.ContentType);
      var contentLength = request.ContentLength;
      var redactedHeaders = RedactionHelper.RedactHeaders(request.Headers);

      var looksEmpty = contentLength == 0 ||
                       (!contentLength.HasValue &&
                        string.IsNullOrWhiteSpace(normalizedContentType) &&
                        !request.Headers.ContainsKey("Transfer-Encoding"));

      if (looksEmpty)
      {
         return (redactedHeaders, new Dictionary<string, object?>());
      }

      if (MediaTypeUtil.IsFormLike(normalizedContentType))
      {
         if (contentLength is null or > LoggingOptions.RequestResponseBodyMaxBytes)
         {
            return (redactedHeaders, LogFormatting.Omitted(
               "form-large-or-unknown",
               contentLength,
               normalizedContentType,
               LoggingOptions.RequestResponseBodyMaxBytes));
         }

         var form = await request.ReadFormAsync(context.RequestAborted);
         return (redactedHeaders, RedactionHelper.RedactFormFields(form));
      }

      if (!MediaTypeUtil.IsTextLike(normalizedContentType) ||
          contentLength is not <= LoggingOptions.RequestResponseBodyMaxBytes)
      {
         return (redactedHeaders, LogFormatting.Omitted(
            "non-text-or-large",
            contentLength,
            normalizedContentType,
            LoggingOptions.RequestResponseBodyMaxBytes));
      }

      request.EnableBuffering();
      return await HttpLogHelper.CaptureAsync(
         request.Body,
         request.Headers,
         normalizedContentType,
         ct);
   }

   private static async Task<(object Headers, object Body)> CaptureResponseAsync(HttpContext context,
      CappedResponseBodyStream tee,
      CancellationToken ct = default)
   {
      var responseContentType = MediaTypeUtil.Normalize(context.Response.ContentType);
      var isText = MediaTypeUtil.IsTextLike(responseContentType);
      var redactedHeaders = RedactionHelper.RedactHeaders(context.Response.Headers);

      if (tee.TotalWritten == 0)
      {
         return (redactedHeaders, new Dictionary<string, object?>());
      }

      if (isText && tee.TotalWritten <= LoggingOptions.RequestResponseBodyMaxBytes)
      {
         // MemoryMarshal avoids the extra heap copy that tee.Captured.ToArray() would incur.
         // TryGetArray always succeeds here since Captured is backed by an ArrayPool byte[].
         Stream memoryStream = MemoryMarshal.TryGetArray(tee.Captured, out var seg)
            ? new MemoryStream(seg.Array!, seg.Offset, seg.Count, writable: false)
            : new MemoryStream(tee.Captured.ToArray());

         await using (memoryStream)
         {
            return await HttpLogHelper.CaptureAsync(
               memoryStream,
               context.Response.Headers,
               responseContentType,
               ct);
         }
      }

      var reason = isText ? "exceeds-limit" : "non-text";
      return (redactedHeaders, LogFormatting.Omitted(
         reason,
         tee.TotalWritten,
         responseContentType,
         LoggingOptions.RequestResponseBodyMaxBytes));
   }

   // All named placeholders become structured properties in Serilog / Elasticsearch.
   // Eliminates the BeginScope dictionary allocation and the LogInformation args-array allocation.
   [LoggerMessage(Level = LogLevel.Information,
      Message = "[HTTP IN] {Method} {Path} -> {StatusCode} in {ElapsedMs}ms | " +
                "{Kind} q={Query} rqH={RequestHeaders} rqB={RequestBody} rsH={ResponseHeaders} rsB={ResponseBody}")]
   private partial void LogHttpIn(
      string method,
      string? path,
      int statusCode,
      double elapsedMs,
      string kind,
      string? query,
      string requestHeaders,
      string requestBody,
      string responseHeaders,
      string responseBody);
}