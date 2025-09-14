using Microsoft.Extensions.Caching.Hybrid;

namespace SharedKernel.Maintenance;

public sealed class MaintenanceState(HybridCache cache)
{
   private const string Key = "maintenance-mode";
   private int _mode = (int)MaintenanceMode.Disabled;

   public MaintenanceMode Mode
   {
      get => (MaintenanceMode)Volatile.Read(ref _mode);
      private set => Volatile.Write(ref _mode, (int)value);
   }

   // for admin/API to change mode (updates local immediately, then L2)
   public async Task SetModeAsync(MaintenanceMode mode, CancellationToken ct = default)
   {
      Mode = mode;
      await cache.SetAsync(
         Key,
         new MaintenanceCacheEntity
         {
            Mode = mode
         },
         new HybridCacheEntryOptions
         {
            Expiration = TimeSpan.MaxValue,
            LocalCacheExpiration = TimeSpan.MaxValue,
            Flags = null
         },
         cancellationToken: ct);
   }

   // used by the poller only
   internal void SetFromPoller(MaintenanceMode mode)
   {
      Mode = mode;
   }
}