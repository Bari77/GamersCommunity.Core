using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GamersCommunity.Core.Database.Seed;

/// <summary>
/// Upserts fixed catalog rows by primary key, with optional IDENTITY_INSERT for SQL Server.
/// </summary>
public abstract class ReferenceTableSeed<TContext, TEntity> : IReferenceTableSeed<TContext>
    where TContext : DbContext
    where TEntity : class
{
    public virtual int Order => 0;

    protected abstract string TableName { get; }

    protected abstract IReadOnlyList<TEntity> Rows { get; }

    /// <summary>Exposed for tests that bootstrap an in-memory catalog without running EnsureAsync.</summary>
    public IReadOnlyList<TEntity> CatalogRows => Rows;

    protected abstract DbSet<TEntity> GetSet(TContext db);

    protected abstract int GetId(TEntity entity);

    public async Task<SeedTotals> EnsureAsync(
        TContext db,
        ILogger logger,
        CancellationToken ct = default)
    {
        var set = GetSet(db);
        var inserted = 0;
        var updated = 0;
        var unchanged = 0;

        foreach (var row in Rows)
        {
            var id = GetId(row);
            var existing = await set.FindAsync([id], ct);
            if (existing is null)
            {
                set.Add(row);
                inserted++;
                logger.LogDebug("Seed {Table}: insert Id={Id}", TableName, id);
                continue;
            }

            var entry = set.Entry(existing);
            entry.CurrentValues.SetValues(row);
            if (entry.Properties.Any(p => p.IsModified))
            {
                updated++;
                logger.LogDebug("Seed {Table}: update Id={Id}", TableName, id);
            }
            else
            {
                unchanged++;
            }
        }

        if (inserted > 0 || updated > 0)
            await SaveWithOptionalIdentityInsertAsync(db, inserted > 0, ct);

        logger.LogInformation(
            "Seed {Table}: {Inserted} inserted, {Updated} updated, {Unchanged} unchanged",
            TableName, inserted, updated, unchanged);

        return new SeedTotals(inserted, updated, unchanged);
    }

    private async Task SaveWithOptionalIdentityInsertAsync(
        TContext db,
        bool identityInsert,
        CancellationToken ct)
    {
        if (!identityInsert)
        {
            await db.SaveChangesAsync(ct);
            return;
        }

        var entityType = db.Model.FindEntityType(typeof(TEntity))
            ?? throw new InvalidOperationException($"Entity type {typeof(TEntity).Name} is not mapped.");
        var table = QuoteIdent(entityType.GetTableName()
            ?? throw new InvalidOperationException($"Entity type {typeof(TEntity).Name} has no table name."));
        var schemaName = entityType.GetSchema();
        var qualified = string.IsNullOrEmpty(schemaName) ? table : QuoteIdent(schemaName) + "." + table;

        await db.Database.OpenConnectionAsync(ct);
        try
        {
            await ExecuteIdentityInsertAsync(db, qualified, enabled: true, ct);
            await db.SaveChangesAsync(ct);
            await ExecuteIdentityInsertAsync(db, qualified, enabled: false, ct);
        }
        finally
        {
            await db.Database.CloseConnectionAsync();
        }
    }

    private static async Task ExecuteIdentityInsertAsync(
        TContext db,
        string qualifiedTable,
        bool enabled,
        CancellationToken ct)
    {
        var connection = db.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = "SET IDENTITY_INSERT " + qualifiedTable + (enabled ? " ON" : " OFF");
        await command.ExecuteNonQueryAsync(ct);
    }

    private static string QuoteIdent(string name)
    {
        if (name.Length == 0 || name.Any(c => !(char.IsLetterOrDigit(c) || c is '_' or ' ')))
            throw new InvalidOperationException($"Unsafe SQL identifier: {name}");
        return "[" + name.Replace("]", "]]") + "]";
    }
}
