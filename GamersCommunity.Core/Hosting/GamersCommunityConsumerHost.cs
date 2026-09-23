using GamersCommunity.Core.Database;
using GamersCommunity.Core.Rabbit;
using GamersCommunity.Core.Realtime;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

namespace GamersCommunity.Core.Hosting;

/// <summary>
/// Shared Consumer microservice host bootstrap (logging hook, Rabbit, DbContext, worker, migrate).
/// </summary>
public static class GamersCommunityConsumerHost
{
    /// <summary>
    /// Registers Rabbit options, SQL DbContext, Serilog singleton, BusRouter, typed consumer, and worker.
    /// </summary>
    public static IServiceCollection AddGamersCommunityConsumerCore<TContext, TConsumer>(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionStringName = "Database")
        where TContext : DbContext
        where TConsumer : BasicServiceConsumer
    {
        services.AddOptions<RabbitMQSettings>()
            .Bind(configuration.GetSection("RabbitMQ"))
            .ValidateOnStart();

        services.AddDbContext<TContext>((_, options) =>
        {
            var connectionString = configuration.GetConnectionString(connectionStringName)
                ?? throw new InvalidOperationException($"Connection string '{connectionStringName}' is missing.");
            options.UseGamersCommunitySqlServer(connectionString);
        });

        services.AddSingleton<Serilog.ILogger>(_ => Log.Logger);
        services.AddScoped<BusRouter>();
        services.AddScoped<TConsumer>();
        services.AddHostedService<ConsumerWorker<TConsumer>>();
        return services;
    }

    /// <summary>
    /// Registers the shared realtime publisher (Gateway fan-out queue).
    /// </summary>
    public static IServiceCollection AddRealtimeEventPublisher(this IServiceCollection services)
    {
        services.AddSingleton<IRealtimeEventPublisher, RealtimeEventPublisher>();
        return services;
    }

    /// <summary>
    /// Builds and runs a Consumer host with shared migrate/seed and fatal logging.
    /// </summary>
    public static async Task RunAsync<TContext, TConsumer>(
        string[] args,
        string consoleTitle,
        Action<HostBuilderContext, ILoggingBuilder> configureLogging,
        Action<HostBuilderContext, IServiceCollection> configureServices,
        Func<TContext, IServiceProvider, CancellationToken, Task>? afterMigrate = null)
        where TContext : DbContext
        where TConsumer : BasicServiceConsumer
    {
        Console.Title = consoleTitle;

        try
        {
            var builder = Host.CreateDefaultBuilder(args)
                .ConfigureLogging(configureLogging)
                .ConfigureServices((context, services) =>
                {
                    services.AddGamersCommunityConsumerCore<TContext, TConsumer>(context.Configuration);
                    configureServices(context, services);
                });

            var host = builder.Build();
            await host.Services.ApplyMigrationsWithRetryAsync<TContext>(afterMigrate);

            var environment = host.Services.GetRequiredService<IHostEnvironment>();
            Log.Information("Started in {Environment} environment...", environment.EnvironmentName);
            await host.RunAsync();
        }
        catch (HostAbortedException ex)
        {
            Log.Fatal(ex, "Aborted.");
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Terminated unexpectedly.");
        }
        finally
        {
            Log.Information("Stopped ...");
        }
    }
}
