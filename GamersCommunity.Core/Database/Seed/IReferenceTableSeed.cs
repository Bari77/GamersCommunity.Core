using Microsoft.Extensions.Logging;

namespace GamersCommunity.Core.Database.Seed;

/// <summary>
/// One reference/catalog table seed applied after EF migrations.
/// </summary>
public interface IReferenceTableSeed<in TContext>
{
    int Order { get; }

    Task<SeedTotals> EnsureAsync(TContext db, ILogger logger, CancellationToken ct = default);
}
