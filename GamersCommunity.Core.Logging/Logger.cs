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
        /// Console theme mapping used by the SystemConsole sink to colorize log output by level and token type.
        /// </summary>
        private static readonly SystemConsoleTheme THEME_SERILOG = new(new Dictionary<ConsoleThemeStyle, SystemConsoleThemeStyle>()
        {
            {
                ConsoleThemeStyle.LevelVerbose, new SystemConsoleThemeStyle
                {
                    Foreground = ConsoleColor.DarkGray,
                }
            },
            {
                ConsoleThemeStyle.LevelDebug, new SystemConsoleThemeStyle
                {
                    Foreground = ConsoleColor.Magenta,
                }
            },
            {
                ConsoleThemeStyle.LevelInformation, new SystemConsoleThemeStyle
                {
                    Foreground = ConsoleColor.Green,
                }
            },
            {
                ConsoleThemeStyle.LevelWarning, new SystemConsoleThemeStyle
                {
                    Foreground = ConsoleColor.DarkYellow,
                }
            },
            {
                ConsoleThemeStyle.LevelError, new SystemConsoleThemeStyle
                {
                    Foreground = ConsoleColor.Red,
                }
            },
            {
                ConsoleThemeStyle.LevelFatal, new SystemConsoleThemeStyle
                {
                    Foreground = ConsoleColor.DarkRed,
                }
            },
            {
                ConsoleThemeStyle.Text, new SystemConsoleThemeStyle
                {
                    Foreground = ConsoleColor.White,
                }
            },
            {
                ConsoleThemeStyle.SecondaryText, new SystemConsoleThemeStyle
                {
                    Foreground = ConsoleColor.DarkCyan,
                }
            },
            {
                ConsoleThemeStyle.TertiaryText, new SystemConsoleThemeStyle
                {
                    Foreground = ConsoleColor.DarkYellow,
                }
            },
            {
                ConsoleThemeStyle.String, new SystemConsoleThemeStyle
                {
                    Foreground = ConsoleColor.White,
                }
            },
            {
                ConsoleThemeStyle.Number, new SystemConsoleThemeStyle
                {
                    Foreground = ConsoleColor.White,
                }
            },
            {
                ConsoleThemeStyle.Boolean, new SystemConsoleThemeStyle
                {
                    Foreground = ConsoleColor.White,
                }
            },
            {
                ConsoleThemeStyle.Null, new SystemConsoleThemeStyle
                {
                    Foreground = ConsoleColor.White,
                }
            },
            {
                ConsoleThemeStyle.Name, new SystemConsoleThemeStyle
                {
                    Foreground = ConsoleColor.White,
                }
            },
            {
                ConsoleThemeStyle.Scalar, new SystemConsoleThemeStyle
                {
                    Foreground = ConsoleColor.White,
                }
            }
        });

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
        /// Two sub-loggers split output: one excluding messages whose template starts with <c>HTTP</c>
        /// (default template), and one including only those (HTTP template). In production, a rolling file
        /// sink is added; when configured, a Seq sink is also enabled.
        /// </para>
        /// </remarks>
        private static LoggerConfiguration GetConfiguration(LoggerSettings config, string applicationName, IHostEnvironment environment)
        {
            Serilog.Debugging.SelfLog.Enable(msg => File.AppendAllText("serilog_errors.txt", msg + "\n"));

            var loggerConfiguration = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Application", applicationName)
                .Enrich.WithProperty("Environment", environment.EnvironmentName)
                .WriteTo.Logger(lc => lc
                    .Filter.ByExcluding(log => log.MessageTemplate.Text.StartsWith("HTTP"))
                    .WriteTo.Console(
                        restrictedToMinimumLevel: LogEventLevel.Verbose,
                        outputTemplate: DEFAULT_TEMPLATE,
                        theme: THEME_SERILOG
                    )
                )
                .WriteTo.Logger(lc => lc
                    .Filter.ByIncludingOnly(log => log.MessageTemplate.Text.StartsWith("HTTP"))
                    .WriteTo.Console(
                        restrictedToMinimumLevel: LogEventLevel.Verbose,
                        outputTemplate: HTTP_TEMPLATE,
                        theme: THEME_SERILOG
                    )
                );

            if (environment.IsProduction())
            {
                loggerConfiguration.WriteTo.File(
                    path: config.FilePath ?? "logs/log-.txt",
                    restrictedToMinimumLevel: LogEventLevel.Verbose,
                    outputTemplate: DEFAULT_TEMPLATE,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7);
            }

            if (!string.IsNullOrEmpty(config.SeqPath)
                && !string.IsNullOrEmpty(config.SeqKey))
            {
                loggerConfiguration.WriteTo.Seq(
                    serverUrl: config.SeqPath,
                    restrictedToMinimumLevel: LogEventLevel.Verbose,
                    apiKey: config.SeqKey);
            }

            return loggerConfiguration;
        }
    }
}
