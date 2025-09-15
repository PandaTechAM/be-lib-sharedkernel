using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Hosting;

namespace SharedKernel.Maintenance;

//This is for local cache entity to poll the maintenance mode from distributed cache
//This should be removed then L1 + L2 cache is implemented in hybrid cache
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