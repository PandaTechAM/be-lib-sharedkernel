using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace SharedKernel.Logging.Middleware;

internal sealed class OutboundLoggingHandler(ILogger<OutboundLoggingHandler> logger) : DelegatingHandler
{
   protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
      CancellationToken cancellationToken)
   {
      var sw = Stopwatch.GetTimestamp();

      var reqHdrDict = HttpLogHelper.CreateHeadersDictionary(request);
      var reqMedia = request.Content?.Headers.ContentType?.MediaType;
      var reqLen = request.Content?.Headers.ContentLength;

      string reqHeaders, reqBody;
      if (!HttpLogHelper.IsTextLike(reqMedia) || reqLen is > LoggingOptions.RequestResponseBodyMaxBytes)
      {
         reqHeaders = JsonSerializer.Serialize(RedactionHelper.RedactHeaders(reqHdrDict));

         var reason = !HttpLogHelper.IsTextLike(reqMedia)
            ? "non-text"
            : reqLen.HasValue
               ? "exceeds-limit"
               : "unknown-length";

         reqBody = JsonSerializer.Serialize(
            HttpLogHelper.BuildOmittedBodyMessage(reason,
               reqLen,
               reqMedia,
               LoggingOptions.RequestResponseBodyMaxBytes));
      }
      else
      {
         (reqHeaders, reqBody) = await HttpLogHelper.CaptureAsync(
            reqHdrDict,
            () => request.Content == null
               ? Task.FromResult(string.Empty)
               : request.Content.ReadAsStringAsync(cancellationToken),
            reqMedia);
      }

      var response = await base.SendAsync(request, cancellationToken);
      var elapsed = Stopwatch.GetElapsedTime(sw)
                             .TotalMilliseconds;

      var resHdrDict = HttpLogHelper.CreateHeadersDictionary(response);
      var resMedia = response.Content.Headers.ContentType?.MediaType;
      var resLen = response.Content.Headers.ContentLength;

      string resHeaders, resBody;
      if (!HttpLogHelper.IsTextLike(resMedia) || resLen is > LoggingOptions.RequestResponseBodyMaxBytes)
      {
         resHeaders = JsonSerializer.Serialize(RedactionHelper.RedactHeaders(resHdrDict));

         var reason = !HttpLogHelper.IsTextLike(resMedia)
            ? "non-text"
            : resLen.HasValue
               ? "exceeds-limit"
               : "unknown-length";

         resBody = JsonSerializer.Serialize(
            HttpLogHelper.BuildOmittedBodyMessage(reason,
               resLen,
               resMedia,
               LoggingOptions.RequestResponseBodyMaxBytes));
      }
      else
      {
         (resHeaders, resBody) = await HttpLogHelper.CaptureAsync(
            resHdrDict,
            () => response.Content.ReadAsStringAsync(cancellationToken),
            resMedia);
      }

      logger.LogInformation(
         "[Outbound Call] HTTP {Method} {Uri} responded with {StatusCode} in {ElapsedMs}ms. " +
         "Request Headers: {RequestHeaders}, Request Body: {RequestBody}, " +
         "Response Headers: {ResponseHeaders}, Response Body: {ResponseBody}",
         request.Method,
         request.RequestUri?.ToString(),
         (int)response.StatusCode,
         elapsed,
         reqHeaders,
         reqBody,
         resHeaders,
         resBody);

      return response;
   }
}