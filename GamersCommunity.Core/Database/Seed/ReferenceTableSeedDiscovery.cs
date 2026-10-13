using System.Reflection;

namespace GamersCommunity.Core.Database.Seed;

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
