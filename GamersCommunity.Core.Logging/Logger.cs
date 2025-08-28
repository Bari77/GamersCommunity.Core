using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;

namespace GamersCommunity.Core.Logging
{
    public static class Logger
    {
        #region Theme definition

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
        /// Default logger template
        /// </summary>
        private static readonly string DEFAULT_TEMPLATE = "[{Timestamp:dd/MM/yyyy HH:mm:ss} - {Level}] [Env:{Environment}] [App:{Application}] - {Message:lj}{NewLine}{Exception}";

        /// <summary>
        /// Http logger template
        /// </summary>
        private static readonly string HTTP_TEMPLATE = "[{Timestamp:dd/MM/yyyy HH:mm:ss} - {Level}] [Env:{Environment}] [App:{Application}] [Ip:{ClientIp}] [Sender:{Sender}] [UserId:{UserId}] - {Message:lj}{NewLine}{Exception}";

        #endregion

        /// <summary>
        /// Initialize a logger
        /// </summary>
        /// <param name="config">Config manager</param>
        /// <param name="applicationName">Application name for logs</param>
        /// <param name="environment">Environment to get variables</param>
        public static void Initialize(LoggerSettings config, string applicationName, IHostEnvironment environment)
        {
            Log.Logger = GetConfiguration(config, applicationName, environment).CreateLogger();
        }

        /// <summary>
        /// Get a base logger configuration
        /// </summary>
        /// <param name="config">Config manager</param>
        /// <param name="applicationName">Application name for logs</param>
        /// <param name="environment">Environment to get variables</param>
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
                loggerConfiguration.WriteTo.File(config.FilePath ?? "logs/log-.txt", LogEventLevel.Verbose, DEFAULT_TEMPLATE, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7);
            }

            if (!string.IsNullOrEmpty(config.SeqPath)
                && !string.IsNullOrEmpty(config.SeqKey))
            {
                loggerConfiguration.WriteTo.Seq(config.SeqPath, LogEventLevel.Verbose, apiKey: config.SeqKey);
            }

            return loggerConfiguration;
        }
    }
}
