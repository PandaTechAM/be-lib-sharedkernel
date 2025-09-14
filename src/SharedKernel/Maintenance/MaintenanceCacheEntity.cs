using MessagePack;

namespace SharedKernel.Maintenance;

[MessagePackObject]
public sealed class MaintenanceCacheEntity
{
   [Key(0)]
   public MaintenanceMode Mode { get; init; }
}