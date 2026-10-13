namespace GamersCommunity.Core.Logging;

/// <summary>
/// Logger settings class
/// </summary>
public class LoggerSettings
{
    /// <summary>
    /// Minimum log level
    /// </summary>
    public LogMinimumLevel MinimumLevel { get; set; } = new LogMinimumLevel();

    /// <summary>
    /// File path
    /// </summary>
    public string? FilePath { get; set; }

    /// <summary>
    /// Seq path
    /// </summary>
    public string? SeqPath { get; set; }

    /// <summary>
    /// Seq API key
    /// </summary>
    public string? SeqKey { get; set; }
}
