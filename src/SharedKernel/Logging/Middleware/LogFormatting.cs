namespace SharedKernel.Logging.Middleware;

internal static class LogFormatting
{
   public static object Omitted(string reason, long? lengthBytes, string? mediaType, int thresholdBytes)
   {
      int? sizeKb = lengthBytes.HasValue ? (int)Math.Round(lengthBytes.Value / 1024d) : null;
      return new Dictionary<string, object?>
      {
         ["omitted"] = true,
         ["reason"] = reason,
         ["sizeKb"] = sizeKb,
         ["thresholdKb"] = thresholdBytes / 1024,
         ["contentType"] = MediaTypeUtil.Normalize(mediaType)
      };
   }
}