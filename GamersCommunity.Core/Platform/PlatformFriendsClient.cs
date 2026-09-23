using System.Collections.Concurrent;
using GamersCommunity.Core.Enums;
using GamersCommunity.Core.Rabbit;
using GamersCommunity.Core.Serialization;
using Serilog;

namespace GamersCommunity.Core.Platform;

public interface IPlatformFriendsClient
{
    /// <summary>
    /// Tells whether the two Platform users are accepted friends.
    /// </summary>
    Task<bool> AreFriendsAsync(
        Guid firstPlatformUserPublicId,
        Guid secondPlatformUserPublicId,
        CancellationToken ct = default);
}

/// <summary>
/// Asks Platform whether two players are friends (e.g. to gate a friends-only page).
/// </summary>
/// <remarks>
/// Fails soft: an unreachable Platform is treated as "not friends" (least privilege).
/// </remarks>
public sealed class PlatformFriendsClient(IRabbitRpcClient rpc, ILogger logger) : IPlatformFriendsClient
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(30);
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();

    public async Task<bool> AreFriendsAsync(
        Guid firstPlatformUserPublicId,
        Guid secondPlatformUserPublicId,
        CancellationToken ct = default)
    {
        if (firstPlatformUserPublicId == Guid.Empty || secondPlatformUserPublicId == Guid.Empty)
            return false;

        if (firstPlatformUserPublicId == secondPlatformUserPublicId)
            return false;

        var (first, second) = firstPlatformUserPublicId.CompareTo(secondPlatformUserPublicId) <= 0
            ? (firstPlatformUserPublicId, secondPlatformUserPublicId)
            : (secondPlatformUserPublicId, firstPlatformUserPublicId);

        var key = $"{first:N}:{second:N}";
        if (_cache.TryGetValue(key, out var cached) && cached.ExpiresAt > DateTime.UtcNow)
            return cached.AreFriends;

        var areFriends = await FetchAreFriendsAsync(first, second, ct);
        _cache[key] = new CacheEntry(areFriends, DateTime.UtcNow.Add(CacheDuration));

        PruneCache();
        return areFriends;
    }

    private async Task<bool> FetchAreFriendsAsync(Guid first, Guid second, CancellationToken ct)
    {
        var payload = JsonSafe.Serialize(new BusMessage
        {
            Type = BusServiceTypeEnum.DATA,
            Resource = "Friends",
            Action = "ARE_FRIENDS",
            Data = JsonSafe.Serialize(new AreFriendsRequest
            {
                FirstUserPublicId = first,
                SecondUserPublicId = second,
            }),
        });

        string response;
        try
        {
            response = await rpc.CallAsync(PlatformQueues.Default, payload, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.Error(ex, "Could not read the Platform friendship; treating the players as strangers.");
            return false;
        }

        return JsonSafe.Deserialize<AreFriendsResult>(response)?.AreFriends ?? false;
    }

    private void PruneCache()
    {
        var now = DateTime.UtcNow;
        foreach (var (key, entry) in _cache)
        {
            if (entry.ExpiresAt <= now)
                _cache.TryRemove(key, out _);
        }
    }

    private sealed record CacheEntry(bool AreFriends, DateTime ExpiresAt);

    private sealed class AreFriendsRequest
    {
        public Guid FirstUserPublicId { get; init; }

        public Guid SecondUserPublicId { get; init; }
    }

    private sealed class AreFriendsResult
    {
        public bool AreFriends { get; init; }
    }
}
