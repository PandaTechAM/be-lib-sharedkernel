using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace SharedKernel.Logging.Middleware;

internal sealed class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
{
   public async Task InvokeAsync(HttpContext context)
   {
      if (HttpMethods.IsOptions(context.Request.Method) ||
          (context.Request.Path.HasValue &&
           LoggingOptions.PathsToIgnore.Any(p => context.Request.Path.StartsWithSegments(p))))
      {
         await next(context);
         return;
      }

      var normReqCt = MediaTypeUtil.Normalize(context.Request.ContentType);
      var reqLen = context.Request.ContentLength;
      var isFormLike =
         string.Equals(normReqCt, "application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase) ||
         string.Equals(normReqCt, "multipart/form-data", StringComparison.OrdinalIgnoreCase);

      var looksEmpty =
         reqLen == 0 ||
         (!reqLen.HasValue && string.IsNullOrWhiteSpace(normReqCt) &&
          !context.Request.Headers.ContainsKey("Transfer-Encoding"));

      object reqHeaders = RedactionHelper.RedactHeaders(context.Request.Headers);
      object reqBody;

      if (looksEmpty)
      {
         reqBody = new Dictionary<string, object?>();
      }
      else if (isFormLike)
      {
         if (reqLen is null or > LoggingOptions.RequestResponseBodyMaxBytes)
         {
            reqBody = LogFormatting.Omitted("form-large-or-unknown",
               reqLen,
               normReqCt,
               LoggingOptions.RequestResponseBodyMaxBytes);
         }
         else
         {
            var form = await context.Request.ReadFormAsync(context.RequestAborted);
            reqBody = RedactionHelper.RedactFormFields(form);
         }
      }
      else if (MediaTypeUtil.IsTextLike(normReqCt) && reqLen is <= LoggingOptions.RequestResponseBodyMaxBytes)
      {
         context.Request.EnableBuffering();
         (reqHeaders, reqBody) = await HttpLogHelper.CaptureAsync(
            context.Request.Body,
            context.Request.Headers,
            normReqCt);
      }
      else
      {
         reqBody = LogFormatting.Omitted("non-text-or-large",
            reqLen,
            normReqCt,
            LoggingOptions.RequestResponseBodyMaxBytes);
      }

      var originalBody = context.Response.Body;
      var tee = new CappedResponseBodyStream(originalBody, LoggingOptions.RequestResponseBodyMaxBytes);
      context.Response.Body = tee;

      var sw = Stopwatch.GetTimestamp();
      try
      {
         await next(context);
      }
      finally
      {
         var elapsed = Stopwatch.GetElapsedTime(sw)
                                .TotalMilliseconds;

         var resCt = MediaTypeUtil.Normalize(context.Response.ContentType);
         var isText = HttpLogHelper.IsTextLike(resCt);

         object resHeaders = RedactionHelper.RedactHeaders(context.Response.Headers);
         object resBody;

         if (tee.TotalWritten == 0)
         {
            resBody = new Dictionary<string, object?>();
         }
         else if (isText && tee.TotalWritten <= LoggingOptions.RequestResponseBodyMaxBytes)
         {
            using var ms = new MemoryStream(tee.Captured.ToArray());
            (resHeaders, resBody) = await HttpLogHelper.CaptureAsync(
               ms,
               context.Response.Headers,
               resCt);
         }
         else
         {
            var reason = isText ? "exceeds-limit" : "non-text";
            resBody = LogFormatting.Omitted(reason,
               tee.TotalWritten,
               resCt,
               LoggingOptions.RequestResponseBodyMaxBytes);
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
            logger.LogInformation(
               "[HTTP IN] {Method} {Path} -> {StatusCode} in {ElapsedMilliseconds}ms",
               context.Request.Method,
               context.Request.Path.Value,
               context.Response.StatusCode,
               elapsed);
         }
      }
   }
}