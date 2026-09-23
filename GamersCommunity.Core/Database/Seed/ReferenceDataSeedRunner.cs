using Microsoft.Extensions.Logging;

namespace GamersCommunity.Core.Database.Seed;

/// <summary>
/// Shared runner for discovered <see cref="IReferenceTableSeed{TContext}"/> tables.
/// </summary>
public static class ReferenceDataSeedRunner
{
    public static async Task EnsureAsync<TContext>(
        TContext db,
        IReadOnlyList<IReferenceTableSeed<TContext>> tables,
        ILogger logger,
        CancellationToken ct = default)
    {
        logger.LogInformation("Reference data seed starting ({TableCount} tables)", tables.Count);
        foreach (var table in tables)
            logger.LogDebug("Seed discovery: {Seed} (Order={Order})", table.GetType().Name, table.Order);

        var totals = SeedTotals.Zero;
        foreach (var table in tables)
            totals += await table.EnsureAsync(db, logger, ct);

        if (totals.HasChanges)
        {
            logger.LogInformation(
                "Reference data seed saved: {Inserted} inserted, {Updated} updated, {Unchanged} unchanged",
                totals.Inserted, totals.Updated, totals.Unchanged);
        }
        else
        {
            logger.LogInformation(
                "Reference data seed already in sync ({Unchanged} rows unchanged)",
                totals.Unchanged);
        }
    }
}
