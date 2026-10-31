using GamersCommunity.Core.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace GamersCommunity.Core.Telemetry;

/// <summary>
/// Registers OpenTelemetry traces and metrics for every GamersCommunity host.
/// Logs stay on Serilog (console, optional Seq, optional OTLP).
/// </summary>
public static class GamersCommunityTelemetryExtensions
{
    /// <summary>
    /// Exports traces and metrics when <see cref="TelemetryExport.ResolveOtlpEndpoint"/> returns an endpoint.
    /// The service name is <see cref="TelemetryExport.ServiceName"/>, which matches the Serilog
    /// <c>Application</c> property once <see cref="Logger.Initialize"/> has run.
    /// </summary>
    /// <param name="services">Application services.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <param name="environment">Host environment.</param>
    public static IServiceCollection AddGamersCommunityTelemetry(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var endpoint = TelemetryExport.ResolveOtlpEndpoint(environment, configuration);
        if (endpoint is null)
            return services;

        var otlp = new Uri(endpoint);

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(TelemetryExport.ServiceName))
            .WithTracing(tracing => tracing
                .AddSource(BusTelemetry.SourceName)
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddSqlClientInstrumentation()
                .AddOtlpExporter(options => ConfigureOtlp(options, otlp)))
            .WithMetrics(metrics => metrics
                .AddRuntimeInstrumentation()
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddMeter(BusTelemetry.MeterName)
                .AddOtlpExporter(options => ConfigureOtlp(options, otlp)));

        return services;
    }

    private static void ConfigureOtlp(OtlpExporterOptions options, Uri endpoint)
    {
        options.Endpoint = endpoint;
        options.Protocol = OtlpExportProtocol.Grpc;
    }
}
