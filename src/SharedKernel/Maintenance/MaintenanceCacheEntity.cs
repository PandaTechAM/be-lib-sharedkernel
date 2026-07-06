using MessagePack;

namespace SharedKernel.Maintenance;

/// <summary>
///     Cache entity used to persist the current maintenance mode value.
/// </summary>
[MessagePackObject]
public sealed class MaintenanceCacheEntity
{
    /// <summary>
    ///     The maintenance mode stored in the cache.
    /// </summary>
    [Key(0)]
    public MaintenanceMode Mode { get; init; }
}
