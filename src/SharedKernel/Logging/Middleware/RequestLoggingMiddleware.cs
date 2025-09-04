using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Logging;

namespace SharedKernel.Logging.Middleware;

internal sealed class RequestLoggingMiddleware(
   RequestDelegate next,
   ILogger<RequestLoggingMiddleware> logger)
{
   public async Task InvokeAsync(HttpContext context)
   {
      // preserve original behavior: ignore OPTIONS and listed prefixes
      if (HttpMethods.IsOptions(context.Request.Method) ||
          (context.Request.Path.HasValue &&
           LoggingOptions.PathsToIgnore.Any(p => context.Request.Path.StartsWithSegments(p))))
      {
         await next(context);
         return;
      }

      // enable request buffering and capture
      context.Request.EnableBuffering();
      var (reqHeaders, reqBody) = await HttpLogHelper.CaptureAsync(
         context.Request.Body,
         context.Request.Headers,
         context.Request.ContentType);

      await using var responseBuffer =
         new Microsoft.AspNetCore.WebUtilities.FileBufferingWriteStream(
            bufferLimit: LoggingOptions.ResponseBufferLimitBytes);

      var originalBody = context.Response.Body;
      context.Response.Body = responseBuffer;

      var sw = Stopwatch.GetTimestamp();
      try
      {
         await next(context);
      }
      finally
      {
         var elapsed = Stopwatch.GetElapsedTime(sw)
                                .TotalMilliseconds;

         var resContentType = context.Response.ContentType;
         var isText = HttpLogHelper.IsTextLike(resContentType);
         var bufferedLen = responseBuffer.Length;

         string resHeaders, resBody;

         if (isText && bufferedLen <= LoggingOptions.RequestResponseBodyMaxBytes)
         {
            using var ms = new MemoryStream(capacity: (int)Math.Min(bufferedLen, int.MaxValue));
            await responseBuffer.DrainBufferAsync(ms, context.RequestAborted);

            ms.Position = 0;
            (resHeaders, resBody) = await HttpLogHelper.CaptureAsync(
               ms,
               context.Response.Headers,
               resContentType);

            ms.Position = 0;
            await ms.CopyToAsync(originalBody, context.RequestAborted);
         }
         else
         {
            resHeaders = JsonSerializer.Serialize(RedactionHelper.RedactHeaders(context.Response.Headers));
            var reason = !isText
               ? "non-text"
               : bufferedLen > LoggingOptions.RequestResponseBodyMaxBytes
                  ? "exceeds-limit"
                  : "unknown-length";

            resBody = JsonSerializer.Serialize(
               HttpLogHelper.BuildOmittedBodyMessage(reason,
                  bufferedLen,
                  resContentType,
                  LoggingOptions.RequestResponseBodyMaxBytes));

            await responseBuffer.DrainBufferAsync(originalBody, context.RequestAborted);
         }

         context.Response.Body = originalBody;

         var scope = new Dictionary<string, object?>
         {
            ["RequestHeaders"] = reqHeaders,
            ["RequestBody"] = reqBody,
            ["ResponseHeaders"] = resHeaders,
            ["ResponseBody"] = resBody,
            ["ElapsedMs"] = elapsed,
            ["Kind"] = "HttpIn"
         };

         if (context.Request.QueryString.HasValue)
         {
            scope["Query"] = context.Request.QueryString.Value;
         }

         using (logger.BeginScope(scope))
         {
            // Distinct, human-readable prefix so it's unmistakably yours in Kibana
            logger.LogInformation(
               "[HTTP IN] {Method} {Path} -> {StatusCode} in {ElapsedMilliseconds}ms",
               context.Request.Method,
               context.Request.Path.Value,
               context.Response.StatusCode,
               elapsed
            );
         }
      }
   }
}