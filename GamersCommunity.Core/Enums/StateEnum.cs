namespace GamersCommunity.Core.Enums
{
    /// <summary>
    /// Describes the lifecycle/change-tracking state of an entity or DTO.
    /// </summary>
    /// <remarks>
    /// Useful for bulk operations, synchronization pipelines, or client-driven CRUD where
    /// each item carries its intended operation. Values are stable and can be serialized.
    /// </remarks>
    /// <example>
    /// <code>
    /// foreach (var item in batch)
    /// {
    ///     switch (item.State)
    ///     {
    ///         case StateEnum.NEW:       await repo.CreateAsync(item);    break;
    ///         case StateEnum.MODIFIED:  await repo.UpdateAsync(item);    break;
    ///         case StateEnum.DELETED:   await repo.DeleteAsync(item.Id); break;
    ///         case StateEnum.UNCHANGED: /* no-op */                      break;
    ///     }
    /// }
    /// </code>
    /// </example>
    public enum StateEnum
    {
        /// <summary>
        /// Entity has not changed; no action is required.
        /// </summary>
        UNCHANGED = 0,

        /// <summary>
        /// Entity is new and should be created/inserted.
        /// </summary>
        NEW = 1,

        /// <summary>
        /// Entity exists and has been updated; persist modifications.
        /// </summary>
        MODIFIED = 2,

        /// <summary>
        /// Entity should be removed/deleted.
        /// </summary>
        DELETED = 3,
    }
}
