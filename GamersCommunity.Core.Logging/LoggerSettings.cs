using Serilog.Events;

namespace GamersCommunity.Core.Logging
{
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

    /// <summary>
    /// Minimum levels for different sinks
    /// </summary>
    public class LogMinimumLevel
    {
        /// <summary>
        /// Global minimum level
        /// </summary>
        public LogEventLevel Global { get; set; } = LogEventLevel.Verbose;
        /// <summary>
        /// Sets the minimum log event level for Console operations with message containing HTTP.
        /// </summary>
        public LogEventLevel ConsoleHttp { get; set; } = LogEventLevel.Verbose;
        /// <summary>
        /// Sets the minimum log event level for Console operations with message not containing HTTP.
        /// </summary>
        public LogEventLevel ConsoleNotHttp { get; set; } = LogEventLevel.Verbose;
        /// <summary>
        /// Sets the minimum log event level for File operations.
        /// </summary>
        public LogEventLevel File { get; set; } = LogEventLevel.Debug;
        /// <summary>
        /// Sets the minimum log event level for Seq operations.
        /// </summary>
        public LogEventLevel Seq { get; set; } = LogEventLevel.Information;
        /// <summary>
        /// Sets the minimum log event level for Entity Framework Core operations.
        /// </summary>
        public LogEventLevel EntityFrameworkCore { get; set; } = LogEventLevel.Warning;
    }
}
