using System.Diagnostics;
using Microsoft.Extensions.Logging;
using SharedKernel.Logging.Helpers;

namespace SharedKernel.Logging;

internal sealed class OutboundLoggingHandler(ILogger<OutboundLoggingHandler> logger)
   : DelegatingHandler
{
   protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
      CancellationToken cancellationToken)
   {
      var sw = Stopwatch.GetTimestamp();

      var (reqHeaders, reqBody) = await HttpLogHelper.CaptureAsync(
         HttpLogHelper.CreateHeadersDictionary(request),
         () => request.Content == null
            ? Task.FromResult(string.Empty)
            : request.Content.ReadAsStringAsync(cancellationToken),
         request.Content?.Headers.ContentType?.MediaType);

      var response = await base.SendAsync(request, cancellationToken);
      var elapsed = Stopwatch.GetElapsedTime(sw)
                             .TotalMilliseconds;

      var (resHeaders, resBody) = await HttpLogHelper.CaptureAsync(
         HttpLogHelper.CreateHeadersDictionary(response),
         () => response.Content.ReadAsStringAsync(cancellationToken),
         response.Content.Headers.ContentType?.MediaType);

      logger.LogInformation(
         "[Outbound Call] HTTP {Method} to {Uri} responded with {StatusCode} in {ElapsedMs}ms. " +
         "Request Headers: {RequestHeaders}, Request Body: {RequestBody}, " +
         "Response Headers: {ResponseHeaders}, Response Body: {ResponseBody}",
         request.Method,
         request.RequestUri,
         (int)response.StatusCode,
         elapsed,
         reqHeaders,
         reqBody,
         resHeaders,
         resBody);

      return response;
   }
}