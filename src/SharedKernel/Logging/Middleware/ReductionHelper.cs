using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;

namespace SharedKernel.Logging.Middleware;

internal static class RedactionHelper
{
    private const string Redacted = "[REDACTED]";

    public static Dictionary<string, string> RedactHeaders(IHeaderDictionary headers) =>
        headers.ToDictionary(
            h => h.Key,
            h => IsSensitiveKey(h.Key) ? Redacted : h.Value.ToString());

    public static Dictionary<string, string> RedactHeaders(Dictionary<string, IEnumerable<string>> headers) =>
        headers.ToDictionary(
            kvp => kvp.Key,
            kvp => IsSensitiveKey(kvp.Key) ? Redacted : string.Join(";", kvp.Value));


    public static object RedactBody(string? contentType, string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return new Dictionary<string, object?>();

        // JSON (including +json)
        if (MediaTypeUtil.IsJson(contentType))
            return RedactJsonBody(raw);

        // application/x-www-form-urlencoded
        if (MediaTypeUtil.IsFormUrlEncoded(contentType))
            return RedactFormUrlEncodedBody(raw);

        // Plain text fallback
        return RedactPlainTextBody(raw);
    }

    private static object RedactJsonBody(string raw)
    {
        try
        {
            var element = JsonSerializer.Deserialize<JsonElement>(raw);
            return RedactElement(element);
        }
        catch (JsonException)
        {
            return new Dictionary<string, object?> { ["invalidJson"] = true };
        }
    }

    private static Dictionary<string, string> RedactFormUrlEncodedBody(string raw)
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var parsed = QueryHelpers.ParseQuery("?" + raw);

        foreach (var kvp in parsed)
        {
            if (string.IsNullOrEmpty(kvp.Key))
                continue;

            var joined = string.Join(";", kvp.Value.ToArray());
            dict[kvp.Key] = RedactFormValue(kvp.Key, joined);
        }

        return dict;
    }

    private static Dictionary<string, object?> RedactPlainTextBody(string raw)
    {
        var rawBytes = Encoding.UTF8.GetByteCount(raw);

        if (rawBytes > LoggingOptions.RedactionMaxPropertyBytes)
        {
            return new Dictionary<string, object?>
            {
                ["text"] = $"[OMITTED: exceeds-limit ~{rawBytes / 1024}KB]"
            };
        }

        var value = ContainsSensitiveKeyword(raw) ? Redacted : raw;
        return new Dictionary<string, object?> { ["text"] = value };
    }


    public static Dictionary<string, string> RedactFormFields(IFormCollection form)
    {
        var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // Text fields
        foreach (var kvp in form)
        {
            var raw = string.Join(";", kvp.Value.ToArray());
            fields[kvp.Key] = RedactFormValue(kvp.Key, raw);
        }

        // File placeholders
        AddFilePlaceholders(form.Files, fields);

        return fields;
    }

    private static void AddFilePlaceholders(IFormFileCollection files, Dictionary<string, string> fields)
    {
        if (files.Count == 0)
            return;

        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var sizes = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);

        foreach (var file in files)
        {
            var key = file.Name;
            counts.TryGetValue(key, out var count);
            counts[key] = count + 1;

            sizes.TryGetValue(key, out var bytes);
            sizes[key] = bytes + file.Length;
        }

        foreach (var key in counts.Keys)
        {
            var count = counts[key];
            var sizeKb = (int)Math.Round(sizes[key] / 1024d);

            var placeholder = count == 1
                ? $"[OMITTED: file {sizeKb}KB]"
                : $"[OMITTED: {count} files {sizeKb}KB]";

            if (fields.TryGetValue(key, out var existing) && !string.IsNullOrWhiteSpace(existing))
                fields[key] = $"{existing}; {placeholder}";
            else
                fields[key] = placeholder;
        }
    }

    internal static string RedactFormValue(string key, string value)
    {
        if (IsSensitiveKey(key) || ContainsSensitiveKeyword(value))
            return Redacted;

        var bytes = Encoding.UTF8.GetByteCount(value);
        return bytes > LoggingOptions.RedactionMaxPropertyBytes
            ? $"[OMITTED: exceeds-limit ~{bytes / 1024}KB]"
            : value;
    }

    private static object RedactElement(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.Object => RedactJsonObject(element),
        JsonValueKind.Array => element.EnumerateArray().Select(RedactElement).ToArray(),
        JsonValueKind.String => RedactString(element.GetString()!),
        JsonValueKind.Number => ParseJsonNumber(element),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Null => null!,
        _ => element.GetRawText()
    };

    private static Dictionary<string, object?> RedactJsonObject(JsonElement element) =>
        element.EnumerateObject().ToDictionary(
            p => p.Name,
            p => IsSensitiveKey(p.Name) ? (object?)Redacted : RedactElement(p.Value));

    private static object ParseJsonNumber(JsonElement element)
    {
        if (element.TryGetInt64(out var i))
            return i;
        if (element.TryGetDouble(out var d))
            return d;
        if (decimal.TryParse(element.GetRawText(), NumberStyles.Any, CultureInfo.InvariantCulture, out var m))
            return m;
        return element.GetRawText();
    }

    private static string RedactString(string value)
    {
        var bytes = Encoding.UTF8.GetByteCount(value);

        if (bytes > LoggingOptions.RedactionMaxPropertyBytes)
            return $"[OMITTED: exceeds-limit ~{bytes / 1024}KB]";

        return ContainsSensitiveKeyword(value) ? Redacted : value;
    }


    private static bool IsSensitiveKey(string key) =>
        LoggingOptions.SensitiveKeywords.Any(k => key.Contains(k, StringComparison.OrdinalIgnoreCase));

    private static bool ContainsSensitiveKeyword(string value) =>
        LoggingOptions.SensitiveKeywords.Any(k => value.Contains(k, StringComparison.OrdinalIgnoreCase));
}