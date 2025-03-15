using Microsoft.Extensions.Hosting;
using Serilog;

namespace SharedKernel.Logging;

public class LogCleanupHostedService(string logsDirectory, TimeSpan retentionPeriod) : BackgroundService
{
   protected override async Task ExecuteAsync(CancellationToken stoppingToken)
   {
      while (!stoppingToken.IsCancellationRequested)
      {
         try
         {
            var files = Directory.EnumerateFiles(logsDirectory, "logs-*.json", SearchOption.TopDirectoryOnly);
            foreach (var file in files)
            {
               var creationTime = File.GetCreationTime(file);
               if (DateTime.UtcNow - creationTime > retentionPeriod)
               {
                  File.Delete(file);
               }
            }
         }
         catch (IOException ex)
         {
            // If 2 instances do the same job at the same time, one of them will throw an exception
            Log.Logger.Information(ex, "Failed to delete log files");
         }
         catch (Exception ex)
         {
            Log.Logger.Error(ex, "Failed to delete logs");
         }

         await Task.Delay(TimeSpan.FromHours(12), stoppingToken);
      }
   }
}