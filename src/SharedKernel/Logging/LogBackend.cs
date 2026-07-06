namespace SharedKernel.Logging;

/// <summary>
///     Log sink backend selection for file-based logging.
/// </summary>
public enum LogBackend
{
    /// <summary>
    ///     No file logging backend is configured.
    /// </summary>
    None = 1,

    /// <summary>
    ///     Writes logs in Elastic Common Schema (ECS) format.
    /// </summary>
    ElasticSearch = 2,

    /// <summary>
    ///     Writes logs in Grafana Loki JSON format.
    /// </summary>
    Loki = 3,

    /// <summary>
    ///     Writes logs in compact JSON format.
    /// </summary>
    CompactJson = 4
}
