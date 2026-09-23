using System.Reflection;
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

/// <summary>
/// Discovers concrete <see cref="IReferenceTableSeed{TContext}"/> implementations in an assembly.
/// </summary>
public static class ReferenceTableSeedDiscovery
{
    public static IReadOnlyList<IReferenceTableSeed<TContext>> Discover<TContext>(Assembly assembly)
    {
        return assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .Where(t => typeof(IReferenceTableSeed<TContext>).IsAssignableFrom(t))
            .Select(t => (IReferenceTableSeed<TContext>)Activator.CreateInstance(t)!)
            .OrderBy(s => s.Order)
            .ThenBy(s => s.GetType().Name, StringComparer.Ordinal)
            .ToArray();
    }
}
