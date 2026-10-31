using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace GamersCommunity.Core.Logging;

/// <summary>
/// Resolves whether a process should export OpenTelemetry, and under which service name.
/// </summary>
public static class TelemetryExport
{
    /// <summary>
    /// Configuration section. <c>Telemetry:OtlpEndpoint</c> overrides the Development default.
    /// </summary>
    public const string SectionName = "Telemetry";

    /// <summary>
    /// OTLP/gRPC endpoint of the local Aspire dashboard.
    /// </summary>
    public const string DevelopmentEndpoint = "http://127.0.0.1:4317";

    /// <summary>
    /// Service name stamped by <see cref="Logger.Initialize"/>, read when the tracer provider starts.
    /// </summary>
    public static string ServiceName =>
        string.IsNullOrWhiteSpace(Logger.ApplicationName) ? "GamersCommunity" : Logger.ApplicationName;

    /// <summary>
    /// Returns the OTLP endpoint to export to, or <c>null</c> when export must stay off.
    /// Development uses <see cref="DevelopmentEndpoint"/> when the setting is empty.
    /// </summary>
    /// <param name="environment">Host environment.</param>
    /// <param name="configuration">Application configuration, when available.</param>
    public static string? ResolveOtlpEndpoint(IHostEnvironment environment, IConfiguration? configuration)
    {
        var configured = configuration?[$"{SectionName}:OtlpEndpoint"];
        if (!string.IsNullOrWhiteSpace(configured))
            return configured.Trim();

        return environment.IsDevelopment() ? DevelopmentEndpoint : null;
    }
}
