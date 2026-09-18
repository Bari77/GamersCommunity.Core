using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GamersCommunity.Core.Database;

/// <summary>
/// Applies EF Core migrations at microservice startup, retrying while SQL Server is unavailable.
/// </summary>
public static class DatabaseMigrationExtensions
{
    private static readonly TimeSpan DefaultRetryDelay = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Runs <see cref="DatabaseFacade.MigrateAsync"/> in a loop until SQL is reachable or a non-transient error occurs.
    /// </summary>
    /// <typeparam name="TContext">The service <see cref="DbContext"/> type.</typeparam>
    /// <param name="services">Root service provider (typically the built host).</param>
    /// <param name="afterMigrate">Optional hook after migrations (reference seed, etc.).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="retryDelay">Delay between retries when SQL is unavailable.</param>
    public static async Task ApplyMigrationsWithRetryAsync<TContext>(
        this IServiceProvider services,
        Func<TContext, IServiceProvider, CancellationToken, Task>? afterMigrate = null,
        CancellationToken cancellationToken = default,
        TimeSpan? retryDelay = null)
        where TContext : DbContext
    {
        var delay = retryDelay ?? DefaultRetryDelay;
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("GamersCommunity.Database");

        logger.LogInformation("Applying database migrations...");

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                using var scope = services.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<TContext>();
                logger.LogInformation("Connecting to SQL...");
                await dbContext.Database.MigrateAsync(cancellationToken);
                logger.LogInformation("Database migrations applied.");

                if (afterMigrate is not null)
                    await afterMigrate(dbContext, scope.ServiceProvider, cancellationToken);

                return;
            }
            catch (Exception ex) when (SqlConnectivity.IsUnavailable(ex))
            {
                logger.LogInformation(
                    "Unable to connect to SQL: {Message}. Retrying in {Delay}s...",
                    SqlConnectivity.GetErrorMessage(ex),
                    delay.TotalSeconds);
                await Task.Delay(delay, cancellationToken);
            }
        }
    }
}
