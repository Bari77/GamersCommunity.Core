using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;

namespace GamersCommunity.Core.Logging
{
    /// <summary>
    /// Centralized Serilog bootstrapper used to configure sinks, minimum levels, enrichers,
    /// and output templates for console/file/Seq targets.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The configuration emits two sub-loggers:
    /// one for general application logs (default template),
    /// and one dedicated to HTTP logs (lines whose message template starts with <c>"HTTP"</c>),
    /// each with its own console output template.
    /// </para>
    /// <para>
    /// Enrichment adds <c>Application</c> and <c>Environment</c> properties to every event.
    /// Serilog internal diagnostics are written to <c>serilog_errors.txt</c> via <see cref="Serilog.Debugging.SelfLog"/>.
    /// In production, a rolling file sink is enabled; a Seq sink is also configured when
    /// <c>SeqPath</c> and <c>SeqKey</c> are provided in <see cref="LoggerSettings"/>.
    /// </para>
    /// </remarks>
    public static class Logger
    {
        #region Theme definition

        /// <summary>
        /// Builds a console theme colorizing the rendered message with <paramref name="message"/>. The timestamp,
        /// the enriched properties, the stack frames and the template punctuation keep a fixed neutral palette,
        /// so that only the message reacts to the level.
        /// </summary>
        /// <param name="message">Color of the message text and of the values interpolated into it.</param>
        /// <returns>A theme usable by the SystemConsole sink.</returns>
        private static SystemConsoleTheme BuildTheme(ConsoleColor message)
        {
            var content = new SystemConsoleThemeStyle { Foreground = message };

            return new SystemConsoleTheme(new Dictionary<ConsoleThemeStyle, SystemConsoleThemeStyle>()
            {
                { ConsoleThemeStyle.LevelVerbose, new SystemConsoleThemeStyle { Foreground = ConsoleColor.DarkGray } },
                { ConsoleThemeStyle.LevelDebug, new SystemConsoleThemeStyle { Foreground = ConsoleColor.Magenta } },
                { ConsoleThemeStyle.LevelInformation, new SystemConsoleThemeStyle { Foreground = ConsoleColor.Green } },
                { ConsoleThemeStyle.LevelWarning, new SystemConsoleThemeStyle { Foreground = ConsoleColor.DarkYellow } },
                { ConsoleThemeStyle.LevelError, new SystemConsoleThemeStyle { Foreground = ConsoleColor.Red } },
                { ConsoleThemeStyle.LevelFatal, new SystemConsoleThemeStyle { Foreground = ConsoleColor.DarkRed } },
                { ConsoleThemeStyle.SecondaryText, new SystemConsoleThemeStyle { Foreground = ConsoleColor.DarkCyan } },
                { ConsoleThemeStyle.TertiaryText, new SystemConsoleThemeStyle { Foreground = ConsoleColor.DarkYellow } },
                { ConsoleThemeStyle.Text, content },
                { ConsoleThemeStyle.String, content },
                { ConsoleThemeStyle.Number, content },
                { ConsoleThemeStyle.Boolean, content },
                { ConsoleThemeStyle.Null, content },
                { ConsoleThemeStyle.Name, content },
                { ConsoleThemeStyle.Scalar, content }
            });
        }

        /// <summary>
        /// Console theme used for events below <see cref="LogEventLevel.Error"/>.
        /// </summary>
        private static readonly SystemConsoleTheme THEME_DEFAULT = BuildTheme(ConsoleColor.White);

        /// <summary>
        /// Console theme used for errors and fatals, so the message stands out in red.
        /// </summary>
        private static readonly SystemConsoleTheme THEME_ERROR = BuildTheme(ConsoleColor.Red);

        /// <summary>
        /// Default console/file output template for non-HTTP events.
        /// Includes timestamp, level, environment, application, message, and exception.
        /// </summary>
        private static readonly string DEFAULT_TEMPLATE = "[{Timestamp:dd/MM/yyyy HH:mm:ss} - {Level}] [Env:{Environment}] [App:{Application}] - {Message:lj}{NewLine}{Exception}";

        /// <summary>
        /// Console output template for HTTP-related events (messages starting with <c>HTTP</c>).
        /// Adds <c>ClientIp</c>, <c>Sender</c>, and <c>UserId</c> if present in the log context.
        /// </summary>
        private static readonly string HTTP_TEMPLATE = "[{Timestamp:dd/MM/yyyy HH:mm:ss} - {Level}] [Env:{Environment}] [App:{Application}] [Ip:{ClientIp}] [Sender:{Sender}] [UserId:{UserId}] - {Message:lj}{NewLine}{Exception}";

        #endregion

        /// <summary>
        /// Initializes the global <see cref="Serilog.Log.Logger"/> instance using the provided settings.
        /// </summary>
        /// <param name="config">Typed logger settings (sinks/options like file path, Seq, etc.).</param>
        /// <param name="applicationName">Application name to enrich as a property on every log event.</param>
        /// <param name="environment">Host environment used to detect production and enrich log events.</param>
        public static void Initialize(LoggerSettings config, string applicationName, IHostEnvironment environment)
        {
            Log.Logger = GetConfiguration(config, applicationName, environment).CreateLogger();
        }

        /// <summary>
        /// Builds the base <see cref="LoggerConfiguration"/> with sinks, enrichers, and level overrides.
        /// </summary>
        /// <param name="config">Typed logger settings.</param>
        /// <param name="applicationName">Application name to enrich into events.</param>
        /// <param name="environment">Current host environment.</param>
        /// <returns>A fully configured <see cref="LoggerConfiguration"/> that can create the root logger.</returns>
        /// <remarks>
        /// <para>
        /// Internal Serilog diagnostics are enabled and written to <c>serilog_errors.txt</c> to aid troubleshooting
        /// sink/formatting issues. The minimum level defaults to <c>Verbose</c> for full control, with a noise
        /// reduction override for <c>Microsoft.EntityFrameworkCore</c>.
        /// </para>
        /// <para>
        /// Console output is split in two by message template — messages starting with <c>HTTP</c> use the HTTP
        /// template, the others the default one — then again by level so that errors and fatals render their
        /// message in red. In production, a rolling file sink is added; when configured, a Seq sink is also enabled.
        /// </para>
        /// </remarks>
        private static LoggerConfiguration GetConfiguration(LoggerSettings config, string applicationName, IHostEnvironment environment)
        {
            Serilog.Debugging.SelfLog.Enable(msg => File.AppendAllText("serilog_errors.txt", msg + "\n"));

            var loggerConfiguration = new LoggerConfiguration()
                .MinimumLevel.Is(config.MinimumLevel.Global)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Application", applicationName)
                .Enrich.WithProperty("Environment", environment.EnvironmentName);

            WriteToThemedConsole(loggerConfiguration, log => !IsHttp(log), config.MinimumLevel.ConsoleNotHttp, DEFAULT_TEMPLATE);
            WriteToThemedConsole(loggerConfiguration, IsHttp, config.MinimumLevel.ConsoleHttp, HTTP_TEMPLATE);

            if (!string.IsNullOrEmpty(config.FilePath))
            {
                loggerConfiguration.WriteTo.File(
                    path: config.FilePath ?? "logs/log-.txt",
                    restrictedToMinimumLevel: config.MinimumLevel.File,
                    outputTemplate: DEFAULT_TEMPLATE,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7);
            }

            if (!string.IsNullOrEmpty(config.SeqPath)
                && !string.IsNullOrEmpty(config.SeqKey))
            {
                loggerConfiguration.WriteTo.Seq(
                    serverUrl: config.SeqPath,
                    restrictedToMinimumLevel: config.MinimumLevel.Seq,
                    apiKey: config.SeqKey);
            }

            return loggerConfiguration;
        }

        /// <summary>
        /// Determines whether an event belongs to the HTTP sub-logger.
        /// </summary>
        /// <param name="log">The event to test.</param>
        /// <returns><c>true</c> when the message template starts with <c>HTTP</c>.</returns>
        private static bool IsHttp(LogEvent log) => log.MessageTemplate.Text.StartsWith("HTTP");

        /// <summary>
        /// Registers a pair of console sinks for the selected events: the default palette below
        /// <see cref="LogEventLevel.Error"/>, the red palette at or above it.
        /// </summary>
        /// <param name="configuration">The configuration to add the sinks to.</param>
        /// <param name="matches">Selects the events handled by this pair.</param>
        /// <param name="minimumLevel">Minimum level accepted by both sinks.</param>
        /// <param name="outputTemplate">Output template shared by both sinks.</param>
        private static void WriteToThemedConsole(
            LoggerConfiguration configuration,
            Func<LogEvent, bool> matches,
            LogEventLevel minimumLevel,
            string outputTemplate)
        {
            configuration
                .WriteTo.Logger(lc => lc
                    .Filter.ByIncludingOnly(log => matches(log) && log.Level < LogEventLevel.Error)
                    .WriteTo.Console(
                        restrictedToMinimumLevel: minimumLevel,
                        outputTemplate: outputTemplate,
                        theme: THEME_DEFAULT
                    )
                )
                .WriteTo.Logger(lc => lc
                    .Filter.ByIncludingOnly(log => matches(log) && log.Level >= LogEventLevel.Error)
                    .WriteTo.Console(
                        restrictedToMinimumLevel: minimumLevel,
                        outputTemplate: outputTemplate,
                        theme: THEME_ERROR
                    )
                );
        }
    }
}
