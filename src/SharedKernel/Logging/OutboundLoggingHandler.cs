using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace SharedKernel.Logging;

internal sealed class OutboundLoggingHandler(ILogger<OutboundLoggingHandler> logger) : DelegatingHandler
{
   protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
      CancellationToken cancellationToken)
   {
      var stopwatch = Stopwatch.GetTimestamp();

      // Capture request
      var requestHeadersDict = CreateHeadersDictionary(request);
      
      var requestHeaders = RedactionHelper.RedactHeaders(requestHeadersDict);
      
      var requestBodyRaw = request.Content == null
         ? string.Empty
         : await request.Content.ReadAsStringAsync(cancellationToken);
      var requestBody = JsonSerializer.Serialize(RedactionHelper.ParseAndRedactJson(requestBodyRaw));

      var response = await base.SendAsync(request, cancellationToken);

      // Capture response
      var elapsedMs = Stopwatch.GetElapsedTime(stopwatch)
                                               .TotalMilliseconds;
      
      var responseHeadersDict = CreateHeadersDictionary(response);
      var responseHeaders = RedactionHelper.RedactHeaders(responseHeadersDict);
      
      var responseBodyRaw = await response.Content.ReadAsStringAsync(cancellationToken);
      var responseBody = JsonSerializer.Serialize(RedactionHelper.ParseAndRedactJson(responseBodyRaw));

      // Log everything
      logger.LogInformation(
         "[Outbound Call] HTTP {Method} to {Uri} responded with {StatusCode} in {ElapsedMs}ms. " +
         "Request Headers: {RequestHeaders}, Request Body: {RequestBody}, " +
         "Response Headers: {ResponseHeaders}, Response Body: {ResponseBody}",
         request.Method,
         request.RequestUri,
         (int)response.StatusCode,
         elapsedMs,
         JsonSerializer.Serialize(requestHeaders),
         requestBody,
         JsonSerializer.Serialize(responseHeaders),
         responseBody);

      return response;
   }
   
   private static Dictionary<string, IEnumerable<string>> CreateHeadersDictionary(HttpRequestMessage request)
   {
      var dict = new Dictionary<string, IEnumerable<string>>(StringComparer.OrdinalIgnoreCase);

      // Request-wide headers
      foreach (var h in request.Headers)
         dict[h.Key] = h.Value;

      // Content headers
      if (request.Content?.Headers == null)
      {
         return dict;
      }


      foreach (var h in request.Content.Headers)
         dict[h.Key] = h.Value;


      return dict;
   }

   private static Dictionary<string, IEnumerable<string>> CreateHeadersDictionary(HttpResponseMessage response)
   {
      var dict = new Dictionary<string, IEnumerable<string>>(StringComparer.OrdinalIgnoreCase);

      // Response-wide headers
      foreach (var h in response.Headers)
         dict[h.Key] = h.Value;


      foreach (var h in response.Content.Headers)
         dict[h.Key] = h.Value;


      return dict;
   }
}