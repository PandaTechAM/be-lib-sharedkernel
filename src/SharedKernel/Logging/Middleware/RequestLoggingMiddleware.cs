using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace SharedKernel.Logging.Middleware;

internal sealed class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
{
   public async Task InvokeAsync(HttpContext context)
   {
      // Skip OPTIONS requests and ignored paths
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
         var elapsedMs = Stopwatch.GetElapsedTime(timestamp)
                                  .TotalMilliseconds;
         var (resHeaders, resBody) = await CaptureResponseAsync(context, tee);

         context.Response.Body = originalBody;

         LogHttpIn(context, reqHeaders, reqBody, resHeaders, resBody, elapsedMs);
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

      // Empty body detection
      var looksEmpty = contentLength == 0 ||
                       (!contentLength.HasValue &&
                        string.IsNullOrWhiteSpace(normalizedContentType) &&
                        !request.Headers.ContainsKey("Transfer-Encoding"));

      if (looksEmpty)
         return (redactedHeaders, new Dictionary<string, object?>());

      // Form content (x-www-form-urlencoded or multipart/form-data)
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

      // Text-like content within size limits
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

      // Non-text or large content
   }

   private static async Task<(object Headers, object Body)> CaptureResponseAsync(HttpContext context,
      CappedResponseBodyStream tee,
      CancellationToken ct = default)
   {
      var responseContentType = MediaTypeUtil.Normalize(context.Response.ContentType);
      var isText = MediaTypeUtil.IsTextLike(responseContentType);
      var redactedHeaders = RedactionHelper.RedactHeaders(context.Response.Headers);

      // Empty response
      if (tee.TotalWritten == 0)
         return (redactedHeaders, new Dictionary<string, object?>());

      // Text response within size limits
      if (isText && tee.TotalWritten <= LoggingOptions.RequestResponseBodyMaxBytes)
      {
         using var memoryStream = new MemoryStream(tee.Captured.ToArray());
         return await HttpLogHelper.CaptureAsync(
            memoryStream,
            context.Response.Headers,
            responseContentType,
            ct);
      }

      // Non-text or large response
      var reason = isText ? "exceeds-limit" : "non-text";
      return (redactedHeaders, LogFormatting.Omitted(
         reason,
         tee.TotalWritten,
         responseContentType,
         LoggingOptions.RequestResponseBodyMaxBytes));
   }

   private void LogHttpIn(HttpContext context,
      object reqHeaders,
      object reqBody,
      object resHeaders,
      object resBody,
      double elapsedMs)
   {
      // Convert bodies to JSON strings to prevent Elasticsearch field explosion
      var scope = new Dictionary<string, object?>
      {
         ["RequestHeaders"] = LogFormatting.ToJsonString(reqHeaders),
         ["RequestBody"] = LogFormatting.ToJsonString(reqBody),
         ["ResponseHeaders"] = LogFormatting.ToJsonString(resHeaders),
         ["ResponseBody"] = LogFormatting.ToJsonString(resBody),
         ["ElapsedMs"] = elapsedMs,
         ["Kind"] = "HttpIn"
      };

      if (context.Request.QueryString.HasValue)
         scope["Query"] = context.Request.QueryString.Value;

      using (logger.BeginScope(scope))
      {
         logger.LogInformation(
            "[HTTP IN] {Method} {Path} -> {StatusCode} in {ElapsedMilliseconds}ms",
            context.Request.Method,
            context.Request.Path.Value,
            context.Response.StatusCode,
            elapsedMs);
      }
   }
}