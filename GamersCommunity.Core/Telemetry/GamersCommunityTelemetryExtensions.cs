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
/// Logs stay on Serilog (console, optional rolling file, optional OTLP).
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
                .AddSource(GamersTelemetry.SourceName)
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddSqlClientInstrumentation()
                .AddOtlpExporter(options => ConfigureOtlp(options, otlp)))
            .WithMetrics(metrics => metrics
                .AddRuntimeInstrumentation()
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddSqlClientInstrumentation()
                .AddMeter(BusTelemetry.MeterName)
                .AddMeter(GamersTelemetry.MeterName)
                .AddView(GamersTelemetry.DurationName, new ExplicitBucketHistogramConfiguration
                {
                    Boundaries = [5, 10, 25, 50, 100, 250, 500, 1000, 2500, 5000],
                })
                .AddView(BusTelemetry.ProcessDurationName, new ExplicitBucketHistogramConfiguration
                {
                    Boundaries = [5, 10, 25, 50, 100, 250, 500, 1000, 2500, 5000],
                })
                .AddView("db.client.operation.duration", new ExplicitBucketHistogramConfiguration
                {
                    Boundaries = [0.005, 0.01, 0.025, 0.05, 0.1, 0.25, 0.5, 1, 2.5, 5],
                    TagKeys = ["db.system.name", "error.type"],
                })
                .AddOtlpExporter(options => ConfigureOtlp(options, otlp)));

        return services;
    }

    private static void ConfigureOtlp(OtlpExporterOptions options, Uri endpoint)
    {
        options.Endpoint = endpoint;
        options.Protocol = OtlpExportProtocol.Grpc;
    }
}
