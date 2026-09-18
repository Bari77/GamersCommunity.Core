using Microsoft.EntityFrameworkCore;

namespace GamersCommunity.Core.Database;

/// <summary>
/// Standard EF Core SQL Server configuration for GamersCommunity microservices.
/// </summary>
public static class DbContextOptionsBuilderExtensions
{
    private const int MaxRetryCount = 5;
    private static readonly TimeSpan MaxRetryDelay = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Configures SQL Server with EF Core execution-strategy retries for transient errors at runtime.
    /// </summary>
    public static DbContextOptionsBuilder UseGamersCommunitySqlServer(
        this DbContextOptionsBuilder optionsBuilder,
        string connectionString)
    {
        return optionsBuilder.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure(
            maxRetryCount: MaxRetryCount,
            maxRetryDelay: MaxRetryDelay,
            errorNumbersToAdd: null));
    }
}
