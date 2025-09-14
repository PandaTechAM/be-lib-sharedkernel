using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Hosting;

namespace SharedKernel.Maintenance;

internal class MaintenanceCachePoller(HybridCache hybridCache, MaintenanceState state) : BackgroundService
{
   protected override async Task ExecuteAsync(CancellationToken stoppingToken)
   {
      while (!stoppingToken.IsCancellationRequested)
      {
         try
         {
            var maintenanceCacheEntity = await hybridCache.GetOrCreateAsync<MaintenanceCacheEntity>(
               "maintenance-mode",
               _ => ValueTask.FromResult(CreateCacheEntity()),
               cancellationToken: stoppingToken);

            state.SetFromPoller(maintenanceCacheEntity.Mode);
         }
         catch
         {
            //ignore
         }
         finally
         {
            await Task.Delay(TimeSpan.FromSeconds(7), stoppingToken);
         }
      }
   }

   private static MaintenanceCacheEntity CreateCacheEntity()
   {
      return new MaintenanceCacheEntity
      {
         Mode = MaintenanceMode.Disabled
      };
   }
}