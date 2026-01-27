using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace SharedKernel.Logging.Middleware;

internal static class HttpLogHelper
{
    public static async Task<(object Headers, object Body)> CaptureAsync(
        Stream bodyStream,
        IHeaderDictionary headers,
        string? contentType,
        CancellationToken ct = default)
    {
        var redactedHeaders = RedactionHelper.RedactHeaders(headers);

        var textLike = MediaTypeUtil.IsTextLike(contentType);
        var hasContentLength = headers.ContainsKey(HeaderNames.ContentLength);
        var contentLength = GetContentLengthOrNull(headers);
        var hasChunked = headers.ContainsKey(HeaderNames.TransferEncoding);

        // Empty body detection
        if ((hasContentLength && contentLength == 0) ||
            (!hasContentLength && !hasChunked && string.IsNullOrWhiteSpace(contentType)))
        {
            return (redactedHeaders, new Dictionary<string, object?>());
        }

        if (!textLike)
        {
            return (redactedHeaders, LogFormatting.Omitted(
                "non-text",
                contentLength,
                MediaTypeUtil.Normalize(contentType),
                LoggingOptions.RequestResponseBodyMaxBytes));
        }

        var (raw, truncated) = await ReadLimitedAsync(bodyStream, LoggingOptions.RequestResponseBodyMaxBytes, ct);

        if (truncated)
        {
            return (redactedHeaders, LogFormatting.Omitted(
                "exceeds-limit",
                LoggingOptions.RequestResponseBodyMaxBytes,
                MediaTypeUtil.Normalize(contentType),
                LoggingOptions.RequestResponseBodyMaxBytes));
        }

        var body = RedactionHelper.RedactBody(contentType, raw);
        return (redactedHeaders, body);
    }

    public static async Task<(object Headers, object Body)> CaptureAsync(
        Dictionary<string, IEnumerable<string>> headers,
        Func<Task<string>> rawReader,
        string? contentType,
        CancellationToken ct = default)
    {
        var redactedHeaders = RedactionHelper.RedactHeaders(headers);

        if (!MediaTypeUtil.IsTextLike(contentType))
            return (redactedHeaders, new Dictionary<string, object?>());

        var raw = await rawReader();
        var byteCount = Encoding.UTF8.GetByteCount(raw);

        if (byteCount > LoggingOptions.RequestResponseBodyMaxBytes)
        {
            return (redactedHeaders, LogFormatting.Omitted(
                "exceeds-limit",
                byteCount,
                MediaTypeUtil.Normalize(contentType),
                LoggingOptions.RequestResponseBodyMaxBytes));
        }

        var body = RedactionHelper.RedactBody(contentType, raw);
        return (redactedHeaders, body);
    }

    public static Dictionary<string, IEnumerable<string>> CreateHeadersDictionary(HttpRequestMessage request)
    {
        var dict = new Dictionary<string, IEnumerable<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var header in request.Headers)
            dict[header.Key] = header.Value;

        if (request.Content?.Headers is { } contentHeaders)
        {
            foreach (var header in contentHeaders)
                dict[header.Key] = header.Value;
        }

        return dict;
    }

    public static Dictionary<string, IEnumerable<string>> CreateHeadersDictionary(HttpResponseMessage response)
    {
        var dict = new Dictionary<string, IEnumerable<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var header in response.Headers)
            dict[header.Key] = header.Value;

        foreach (var header in response.Content.Headers)
            dict[header.Key] = header.Value;

        return dict;
    }

    private static long? GetContentLengthOrNull(IHeaderDictionary headers)
    {
        if (headers.TryGetValue(HeaderNames.ContentLength, out var value) &&
            long.TryParse(value.ToString(), out var contentLength))
        {
            return contentLength;
        }
        return null;
    }

    private static async Task<(string text, bool truncated)> ReadLimitedAsync(
        Stream stream,
        int maxBytes,
        CancellationToken ct = default)
    {
        stream.Seek(0, SeekOrigin.Begin);

        using var memoryStream = new MemoryStream(maxBytes);
        var buffer = new byte[Math.Min(8192, maxBytes)];
        var totalRead = 0;

        while (totalRead < maxBytes)
        {
            var toRead = Math.Min(buffer.Length, maxBytes - totalRead);
            var bytesRead = await stream.ReadAsync(buffer.AsMemory(0, toRead), ct);

            if (bytesRead == 0)
                break;

            await memoryStream.WriteAsync(buffer.AsMemory(0, bytesRead), ct);
            totalRead += bytesRead;
        }

        var truncated = false;

        if (totalRead == maxBytes)
        {
            var probe = new byte[1];
            var probeRead = await stream.ReadAsync(probe.AsMemory(0, 1), ct);

            if (probeRead > 0)
            {
                truncated = true;
                if (stream.CanSeek)
                    stream.Seek(-probeRead, SeekOrigin.Current);
            }
        }

        stream.Seek(0, SeekOrigin.Begin);
        return (Encoding.UTF8.GetString(memoryStream.ToArray()), truncated);
    }
}