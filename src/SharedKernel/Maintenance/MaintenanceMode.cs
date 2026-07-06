namespace SharedKernel.Maintenance;

/// <summary>
///     Represents the current maintenance mode state of the application.
/// </summary>
public enum MaintenanceMode
{
    /// <summary>
    ///     Maintenance mode is off; all requests are served normally.
    /// </summary>
    Disabled = 0,

    /// <summary>
    ///     Blocks non-admin routes while continuing to serve admin routes.
    /// </summary>
    EnabledForClients = 1,

    /// <summary>
    ///     Blocks all routes except the infrastructure endpoints under /above-board/.
    /// </summary>
    EnabledForAll = 2
}
