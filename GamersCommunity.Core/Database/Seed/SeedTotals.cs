namespace GamersCommunity.Core.Database.Seed;

public readonly record struct SeedTotals(int Inserted, int Updated, int Unchanged)
{
    public static SeedTotals Zero => new(0, 0, 0);

    public bool HasChanges => Inserted > 0 || Updated > 0;

    public static SeedTotals operator +(SeedTotals left, SeedTotals right) =>
        new(left.Inserted + right.Inserted, left.Updated + right.Updated, left.Unchanged + right.Unchanged);
}
