using GamersCommunity.Core.Enums;
using GamersCommunity.Core.Rabbit;
using GamersCommunity.Core.Serialization;
using Serilog;

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

/// <summary>
/// Asks Platform to keep a managed Whispers channel in sync with a game roster.
/// </summary>
public sealed class PlatformConversationsClient(IRabbitRpcClient rpc, ILogger logger)
    : IPlatformConversationsClient
{
    public Task EnsureManagedChannelAsync(
        string managedKey,
        string title,
        string? pictureUrl,
        Guid ownerUserPublicId,
        CancellationToken ct = default) =>
        CallVoidAsync("ENSURE_GUILD", new
        {
            ManagedKey = managedKey,
            Title = title,
            PictureUrl = pictureUrl,
            OwnerUserPublicId = ownerUserPublicId,
        }, ct);

    public Task AddManagedChannelMemberAsync(string managedKey, Guid userPublicId, CancellationToken ct = default) =>
        CallVoidAsync("ADD_GUILD_MEMBER", new { ManagedKey = managedKey, UserPublicId = userPublicId }, ct);

    public Task RemoveManagedChannelMemberAsync(string managedKey, Guid userPublicId, CancellationToken ct = default) =>
        CallVoidAsync("REMOVE_GUILD_MEMBER", new { ManagedKey = managedKey, UserPublicId = userPublicId }, ct);

    public Task DeleteManagedChannelAsync(string managedKey, CancellationToken ct = default) =>
        CallVoidAsync("DELETE_GUILD", new { ManagedKey = managedKey }, ct);

    private async Task CallVoidAsync(string action, object data, CancellationToken ct)
    {
        var payload = JsonSafe.Serialize(new BusMessage
        {
            Type = BusServiceTypeEnum.DATA,
            Resource = "Conversations",
            Action = action,
            Data = JsonSafe.Serialize(data),
        });

        try
        {
            await rpc.CallAsync(PlatformQueues.Default, payload, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.Error(ex, "Platform did not apply managed Whispers action {Action}.", action);
            throw;
        }
    }
}
