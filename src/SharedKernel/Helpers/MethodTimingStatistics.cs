using System.Diagnostics;
using Microsoft.Extensions.Logging;

#warning "Not recommended for production use. Just for benchmarking purposes."

namespace SharedKernel.Helpers;

/// <summary>
/// Tracks and logs timing statistics of method executions for benchmarking.
/// </summary>
/// <remarks>
/// Not recommended for production usage. Use only for performance diagnostics or quick benchmarks.
/// </remarks>
public class MethodTimingStatistics
{
   public required string MethodName { get; init; }
   private int InvocationCount { get; set; }
   private double TotalElapsedMilliseconds { get; set; }
   private double AverageElapsedMilliseconds { get; set; }

   private static readonly List<MethodTimingStatistics> CollectedStats = [];

   /// <summary>
   /// Updates or adds statistics for a particular method based on the start timestamp.
   /// </summary>
   /// <param name="methodName">The name of the method being tracked.</param>
   /// <param name="startTimestamp">A timestamp (via <see cref="Stopwatch.GetTimestamp"/>) captured before the method execution.</param>
   public static void RecordExecution(string methodName, long startTimestamp)
   {
      var elapsedMs = Stopwatch.GetElapsedTime(startTimestamp)
                               .TotalMilliseconds;

      var existingStat = CollectedStats.FirstOrDefault(s => s.MethodName == methodName);
      if (existingStat == null)
      {
         CollectedStats.Add(new MethodTimingStatistics
         {
            MethodName = methodName,
            InvocationCount = 1,
            TotalElapsedMilliseconds = elapsedMs,
            AverageElapsedMilliseconds = elapsedMs
         });
      }
      else
      {
         existingStat.InvocationCount++;
         existingStat.TotalElapsedMilliseconds += elapsedMs;
         existingStat.AverageElapsedMilliseconds =
            existingStat.TotalElapsedMilliseconds / existingStat.InvocationCount;
      }
   }

   /// <summary>
   /// Logs the accumulated statistics for all tracked methods.
   /// </summary>
   /// <param name="logger">An <see cref="ILogger"/> instance used for logging the statistics.</param>
   public static void LogAll(ILogger logger)
   {
      foreach (var stat in CollectedStats)
      {
         var avgFormatted = FormatDuration(stat.AverageElapsedMilliseconds);
         var totalFormatted = FormatDuration(stat.TotalElapsedMilliseconds);

         logger.LogInformation(
            "Method '{MethodName}' statistics => Called {Count} times, Average duration: {Avg}, Total elapsed: {Total}.",
            stat.MethodName,
            stat.InvocationCount,
            avgFormatted,
            totalFormatted);
      }
   }

   /// <summary>
   /// Clears the statistics for a specific method or for all methods if none is specified.
   /// </summary>
   /// <param name="methodName">Optional method name to clear; if null or empty, clears all statistics.</param>
   public static void ClearStatistics(string? methodName = null)
   {
      if (!string.IsNullOrEmpty(methodName))
      {
         CollectedStats.RemoveAll(s => s.MethodName == methodName);
      }
      else
      {
         CollectedStats.Clear();
      }
   }
   private static string FormatDuration(double milliseconds)
   {
      switch (milliseconds)
      {
         // Less than 1 ms => microseconds
         case < 1:
         {
            var microseconds = milliseconds * 1000.0;
            return $"{microseconds:F3} µs";
         }
         // 1 ms up to 1000 ms
         case < 1000:
            return $"{milliseconds:F3} ms";
      }

      // Convert to seconds
      var seconds = milliseconds / 1000.0;

      // If < 60 seconds => show "X s Y ms"
      if (seconds < 60)
      {
         var wholeSeconds = (int)seconds;
         var remainderMs = milliseconds - wholeSeconds * 1000.0;
         return $"{wholeSeconds}s {remainderMs:F3}ms";
      }

      // Otherwise => show "X min Y s" (limit to minutes)
      var minutes = (int)(seconds / 60);
      var remainSeconds = (int)(seconds % 60);
      return $"{minutes}m {remainSeconds}s";
   }
}