using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Logging;
using SharedKernel.Logging.Helpers;

namespace SharedKernel.Logging;

internal sealed class RequestLoggingMiddleware(
    RequestDelegate next,
    ILogger<RequestLoggingMiddleware> logger)
{
    private static readonly HashSet<string> PathsToIgnore = new(StringComparer.OrdinalIgnoreCase)
    {
        "/openapi",
        "/above-board"
    };

    public async Task InvokeAsync(HttpContext context)
    {
        if (HttpMethods.IsOptions(context.Request.Method) ||
            (context.Request.Path.HasValue &&
             PathsToIgnore.Any(p => context.Request.Path.StartsWithSegments(p))))
        {
            await next(context);
            return;
        }

        context.Request.EnableBuffering();
        await using var responseBody = new MemoryStream();
        var originalBody = context.Response.Body;
        context.Response.Body = responseBody;

        var sw = Stopwatch.GetTimestamp();
        var (reqHeaders, reqBody) = await HttpLogHelper.CaptureAsync(
            context.Request.Body,
            context.Request.Headers,
            context.Request.ContentType);

        try
        {
            await next(context);
        }
        finally
        {
            var elapsed = Stopwatch.GetElapsedTime(sw).TotalMilliseconds;
            var (resHeaders, resBody) = await HttpLogHelper.CaptureAsync(
                responseBody,
                context.Response.Headers,
                context.Response.ContentType);

            responseBody.Seek(0, SeekOrigin.Begin);
            await responseBody.CopyToAsync(originalBody);

            logger.LogInformation(
                "[Incoming Request] HTTP {Method} {Url} responded with {StatusCode} in {ElapsedMilliseconds}ms. " +
                "Request Headers: {RequestHeaders}, Request Body: {RequestBody}, " +
                "Response Headers: {ResponseHeaders}, Response Body: {ResponseBody}",
                context.Request.Method,
                context.Request.GetDisplayUrl(),
                context.Response.StatusCode,
                elapsed,
                reqHeaders,
                reqBody,
                resHeaders,
                resBody);
        }
    }
}
