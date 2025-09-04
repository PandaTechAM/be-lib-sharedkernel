using System.Diagnostics;
using Microsoft.AspNetCore.WebUtilities;
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

      object reqHeaders;
      object reqBody;

      var isFormLike =
         string.Equals(reqMedia, "application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase) ||
         string.Equals(reqMedia, "multipart/form-data", StringComparison.OrdinalIgnoreCase);

      if (request.Content is null)
      {
         reqHeaders = RedactionHelper.RedactHeaders(reqHdrDict);
         reqBody = new Dictionary<string, object?>();
      }
      else if (isFormLike)
      {
         reqHeaders = RedactionHelper.RedactHeaders(reqHdrDict);

         if (reqLen is null or > LoggingOptions.RequestResponseBodyMaxBytes)
         {
            reqBody = LogFormatting.Omitted("form-large-or-unknown",
               reqLen,
               reqMedia,
               LoggingOptions.RequestResponseBodyMaxBytes);
         }
         else
         {
            var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            switch (request.Content)
            {
               case FormUrlEncodedContent:
               {
                  var raw = await request.Content.ReadAsStringAsync(cancellationToken);
                  var parsed = QueryHelpers.ParseQuery("?" + raw);
                  foreach (var kvp in parsed)
                  {
                     var k = kvp.Key;
                     if (string.IsNullOrEmpty(k))
                     {
                        continue;
                     }

                     fields[k] = RedactionHelper.RedactFormValue(k, string.Join(";", kvp.Value.ToArray()));
                  }

                  break;
               }
               case MultipartFormDataContent mfd:
               {
                  var fileCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                  var fileSizes = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);

                  foreach (var part in mfd)
                  {
                     var cd = part.Headers.ContentDisposition;
                     var name = cd?.Name?.Trim('"') ?? "field";
                     var isFile = cd != null &&
                                  (!string.IsNullOrEmpty(cd.FileName) || !string.IsNullOrEmpty(cd.FileNameStar));

                     if (isFile)
                     {
                        fileCounts.TryGetValue(name, out var c);
                        fileCounts[name] = c + 1;

                        if (part.Headers.ContentLength.HasValue)
                        {
                           fileSizes.TryGetValue(name, out var b);
                           fileSizes[name] = b + part.Headers.ContentLength.Value;
                        }

                        continue;
                     }

                     var value = await part.ReadAsStringAsync(cancellationToken);
                     fields[name] = RedactionHelper.RedactFormValue(name, value);
                  }

                  foreach (var name in fileCounts.Keys)
                  {
                     var count = fileCounts[name];
                     fileSizes.TryGetValue(name, out var bytes);
                     var hasSize = bytes > 0;
                     var sizeKb = hasSize ? (int)Math.Round(bytes / 1024d) : (int?)null;

                     var place = count == 1
                        ? hasSize ? $"[OMITTED: file {sizeKb}KB]" : "[OMITTED: file]"
                        : hasSize
                           ? $"[OMITTED: {count} files {sizeKb}KB]"
                           : $"[OMITTED: {count} files]";

                     if (fields.TryGetValue(name, out var existing) && !string.IsNullOrWhiteSpace(existing))
                     {
                        fields[name] = $"{existing}; {place}";
                     }
                     else
                     {
                        fields[name] = place;
                     }
                  }

                  break;
               }
            }

            reqBody = fields;
         }
      }
      else if (!HttpLogHelper.IsTextLike(reqMedia) || !reqLen.HasValue ||
               reqLen.Value > LoggingOptions.RequestResponseBodyMaxBytes)
      {
         reqHeaders = RedactionHelper.RedactHeaders(reqHdrDict);
         var reason = !HttpLogHelper.IsTextLike(reqMedia) ? "non-text" : "exceeds-limit-or-unknown";
         reqBody = LogFormatting.Omitted(reason, reqLen, reqMedia, LoggingOptions.RequestResponseBodyMaxBytes);
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

      object resHeaders;
      object resBody;

      if (!HttpLogHelper.IsTextLike(resMedia) || !resLen.HasValue ||
          resLen.Value > LoggingOptions.RequestResponseBodyMaxBytes)
      {
         resHeaders = RedactionHelper.RedactHeaders(resHdrDict);

         if (resLen == 0)
         {
            resBody = new Dictionary<string, object?>();
         }
         else
         {
            var reason = !HttpLogHelper.IsTextLike(resMedia) ? "non-text" : "exceeds-limit-or-unknown";
            resBody = LogFormatting.Omitted(reason, resLen, resMedia, LoggingOptions.RequestResponseBodyMaxBytes);
         }
      }
      else
      {
         (resHeaders, resBody) = await HttpLogHelper.CaptureAsync(
            resHdrDict,
            () => response.Content.ReadAsStringAsync(cancellationToken),
            resMedia);
      }

      var hostPath = request.RequestUri is null ? "" : request.RequestUri.GetLeftPart(UriPartial.Path);

      var scope = new Dictionary<string, object?>
      {
         ["RequestHeaders"] = reqHeaders,
         ["RequestBody"] = reqBody,
         ["ResponseHeaders"] = resHeaders,
         ["ResponseBody"] = resBody,
         ["ElapsedMs"] = elapsed,
         ["Kind"] = "HttpOut"
      };

      if (!string.IsNullOrEmpty(request.RequestUri?.Query))
      {
         scope["Query"] = request.RequestUri!.Query;
      }

      using (logger.BeginScope(scope))
      {
         logger.LogInformation(
            "[HTTP OUT] {Method} {HostPath} -> {StatusCode} in {ElapsedMilliseconds}ms",
            request.Method,
            hostPath,
            (int)response.StatusCode,
            elapsed);
      }

      return response;
   }
}