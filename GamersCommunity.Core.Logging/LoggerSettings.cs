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
    /// Rolling file path. When empty, no file sink is registered.
    /// </summary>
    public string? FilePath { get; set; }
}
