using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace SharedKernel.Logging;

internal sealed class RequestLoggingMiddleware(
   RequestDelegate next,
   ILogger<RequestLoggingMiddleware> logger)
{
   public async Task InvokeAsync(HttpContext context)
   {
      if (HttpMethods.IsOptions(context.Request.Method))
      {
         await next(context);
         return;
      }

      var requestLog = await CaptureRequestAsync(context.Request);
      var originalBodyStream = context.Response.Body;

      await using var responseBody = new MemoryStream();
      context.Response.Body = responseBody;

      var stopwatch = Stopwatch.GetTimestamp();

      try
      {
         await next(context);
      }
      finally
      {
         var elapsedMs = Stopwatch.GetElapsedTime(stopwatch)
                                  .TotalMilliseconds;
         var responseLog = await CaptureResponseAsync(context.Response);

         logger.LogInformation(
            "[Incoming Request] HTTP {Method} {Query} responded with {StatusCode} in {ElapsedMilliseconds}ms. " +
            "Request Headers: {RequestHeaders}, Request Body: {RequestBody}, " +
            "Response Headers: {ResponseHeaders}, Response Body: {ResponseBody}",
            context.Request.Method,
            context.Request.QueryString,
            context.Response.StatusCode,
            elapsedMs,
            requestLog.Headers,
            requestLog.Body,
            responseLog.Headers,
            responseLog.Body);

         await responseBody.CopyToAsync(originalBodyStream);
      }
   }

   private static async Task<(string Headers, string Body)> CaptureRequestAsync(HttpRequest request)
   {
      request.EnableBuffering();
      var (headers, bodyContent) = await CaptureLogAsync(request.Body, request.Headers);
      request.Body.Position = 0;
      return (headers, bodyContent);
   }

   private static async Task<(string Headers, string Body)> CaptureResponseAsync(HttpResponse response)
   {
      response.Body.Seek(0, SeekOrigin.Begin);
      var (headers, bodyContent) = await CaptureLogAsync(response.Body, response.Headers);
      response.Body.Seek(0, SeekOrigin.Begin);
      return (headers, bodyContent);
   }

   private static async Task<(string Headers, string Body)> CaptureLogAsync(Stream bodyStream,
      IHeaderDictionary headers)
   {
      using var reader = new StreamReader(bodyStream, leaveOpen: true);
      var body = await reader.ReadToEndAsync();

      var sanitizedHeaders = RedactionHelper.RedactHeaders(headers);
      var redactedBody = RedactionHelper.ParseAndRedactJson(body);

      return (
         JsonSerializer.Serialize(sanitizedHeaders),
         JsonSerializer.Serialize(redactedBody)
      );
   }
}