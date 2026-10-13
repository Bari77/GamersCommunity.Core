namespace GamersCommunity.Core.Platform;

public interface IPlatformConversationsClient
{
    /// <summary>
    /// Creates or refreshes a Platform-managed Whispers channel (guild/team/etc.).
    /// Platform bus action remains <c>ENSURE_GUILD</c>.
    /// </summary>
    Task EnsureManagedChannelAsync(
        string managedKey,
        string title,
        string? pictureUrl,
        Guid ownerUserPublicId,
        CancellationToken ct = default);

    Task AddManagedChannelMemberAsync(string managedKey, Guid userPublicId, CancellationToken ct = default);

    Task RemoveManagedChannelMemberAsync(string managedKey, Guid userPublicId, CancellationToken ct = default);

    Task DeleteManagedChannelAsync(string managedKey, CancellationToken ct = default);

    /// <inheritdoc cref="EnsureManagedChannelAsync"/>
    Task EnsureGuildChannelAsync(
        string managedKey,
        string title,
        string? pictureUrl,
        Guid ownerUserPublicId,
        CancellationToken ct = default) =>
        EnsureManagedChannelAsync(managedKey, title, pictureUrl, ownerUserPublicId, ct);

    Task AddGuildMemberAsync(string managedKey, Guid userPublicId, CancellationToken ct = default) =>
        AddManagedChannelMemberAsync(managedKey, userPublicId, ct);

    Task RemoveGuildMemberAsync(string managedKey, Guid userPublicId, CancellationToken ct = default) =>
        RemoveManagedChannelMemberAsync(managedKey, userPublicId, ct);

    Task DeleteGuildChannelAsync(string managedKey, CancellationToken ct = default) =>
        DeleteManagedChannelAsync(managedKey, ct);
}
