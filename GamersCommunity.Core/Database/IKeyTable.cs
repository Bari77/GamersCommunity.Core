namespace GamersCommunity.Core.Database
{
    /// <summary>
    /// Contract for persisted entities that expose a primary key and basic audit timestamps.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Implement this interface on entities stored in the database to provide a consistent shape for
    /// generic CRUD services and repositories. The <see cref="Id"/> is treated as the primary key.
    /// </para>
    /// <para>
    /// <see cref="CreationDate"/> and <see cref="ModificationDate"/> should be maintained by the data layer
    /// (e.g., EF Core SaveChanges interceptor) and are recommended to be stored in UTC.
    /// </para>
    /// </remarks>
    public interface IKeyTable
    {
        /// <summary>
        /// Primary key identifier of the entity.
        /// </summary>
        int Id { get; set; }

        /// <summary>
        /// Timestamp when the entity was created (recommended UTC).
        /// </summary>
        DateTime CreationDate { get; set; }

        /// <summary>
        /// Timestamp when the entity was last modified (recommended UTC).
        /// </summary>
        DateTime ModificationDate { get; set; }
    }
}
