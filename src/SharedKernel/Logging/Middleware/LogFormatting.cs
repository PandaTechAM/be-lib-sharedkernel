using System.Text.Json;

namespace SharedKernel.Logging.Middleware;

internal static class LogFormatting
{
   public static string Json(object? o) => JsonSerializer.Serialize(o);

   public static string Omitted(string reason, long? lenBytes, string? mediaType, int thresholdBytes)
   {
      var thresholdKb = thresholdBytes / 1024;
      var len = lenBytes.HasValue ? $"{lenBytes.Value}B (~{lenBytes.Value / 1024}KB)" : "unknown";
      var ct = string.IsNullOrWhiteSpace(mediaType) ? "unknown" : mediaType;
      return $"[OMITTED] reason={reason}; length={len}; threshold={thresholdKb}KB; contentType={ct}";
   }
}