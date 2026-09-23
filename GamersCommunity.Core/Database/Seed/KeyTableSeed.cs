namespace GamersCommunity.Core.Database.Seed;

/// <summary>
/// Seed base for entities implementing <see cref="IKeyTable"/>.
/// </summary>
public abstract class KeyTableSeed<TContext, TEntity> : ReferenceTableSeed<TContext, TEntity>
    where TContext : Microsoft.EntityFrameworkCore.DbContext
    where TEntity : class, IKeyTable
{
    protected sealed override int GetId(TEntity entity) => entity.Id;
}
