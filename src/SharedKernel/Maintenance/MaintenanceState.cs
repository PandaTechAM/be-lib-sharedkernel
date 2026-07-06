using Microsoft.Extensions.Caching.Hybrid;

namespace SharedKernel.Maintenance;

// This is a local cache entity to hold the maintenance mode in memory
// This should be removed then L1 + L2 cache is implemented in hybrid cache
// thread-safe local snapshot
/// <summary>
///     Tracks the current maintenance mode as a thread-safe in-memory snapshot backed by the distributed cache.
/// </summary>
/// <param name="cache">The hybrid cache used to persist and synchronize the maintenance mode across instances.</param>
public sealed class MaintenanceState(HybridCache cache)
{
    private const string Key = "maintenance-mode";
    private int _mode = (int)MaintenanceMode.Disabled;

    /// <summary>
    ///     The current maintenance mode.
    /// </summary>
    public MaintenanceMode Mode
    {
        get => (MaintenanceMode)Volatile.Read(ref _mode);
        private set => Volatile.Write(ref _mode, (int)value);
    }

    // for admin/API to change mode (updates local immediately, then L2)
    /// <summary>
    ///     Updates the maintenance mode locally and persists it to the distributed cache.
    /// </summary>
    public async Task SetModeAsync(MaintenanceMode mode, CancellationToken ct = default)
    {
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

        Mode = mode;
    }

    // used by the poller only
    internal void SetFromPoller(MaintenanceMode mode)
    {
        Mode = mode;
    }
}
