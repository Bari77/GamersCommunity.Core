using System.Collections.Concurrent;
using GamersCommunity.Core.Enums;
using GamersCommunity.Core.Exceptions;
using GamersCommunity.Core.Rabbit;
using GamersCommunity.Core.Serialization;
using Serilog;

namespace GamersCommunity.Core.Platform;

public interface IPlatformSanctionsClient
{
    /// <summary>
    /// Refuses the call when the caller is muted or banned on Platform.
    /// </summary>
    Task EnsureCanPublishAsync(BusMessage message, CancellationToken ct = default);
}

/// <summary>
/// Asks Platform, on every publication, whether the caller is allowed to speak.
/// </summary>
/// <remarks>
/// Sanctions belong to Platform and must never be replicated into game databases.
/// The check is a synchronous RPC on <see cref="PlatformQueues.Default"/> with a short-lived cache.
/// Fails closed: an unreachable Platform blocks publications.
/// </remarks>
public sealed class PlatformSanctionsClient(IRabbitRpcClient rpc, ILogger logger) : IPlatformSanctionsClient
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(30);
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();

    public async Task EnsureCanPublishAsync(BusMessage message, CancellationToken ct = default)
    {
        if (message.Caller?.Subject is not { } subject || string.IsNullOrWhiteSpace(subject))
            throw new UnauthorizedException("UNAUTHORIZED", "Authenticated caller required");

        var sanctions = await GetSanctionsAsync(subject, message.Caller, ct);

        if (sanctions.Banned)
            throw new ForbiddenException("BANNED", "Banned account");

        if (sanctions.ActiveMute is { } mute)
        {
            throw new ForbiddenException(
                "MUTED",
                $"You are muted until {mute.EndDate:u}: {mute.Reason}");
        }
    }

    private async Task<CallerSanctions> GetSanctionsAsync(
        string subject,
        CallerIdentity caller,
        CancellationToken ct)
    {
        if (_cache.TryGetValue(subject, out var cached) && cached.ExpiresAt > DateTime.UtcNow)
            return cached.Sanctions;

        var sanctions = await FetchSanctionsAsync(caller, ct);
        _cache[subject] = new CacheEntry(sanctions, DateTime.UtcNow.Add(CacheDuration));

        PruneCache();
        return sanctions;
    }

    private async Task<CallerSanctions> FetchSanctionsAsync(CallerIdentity caller, CancellationToken ct)
    {
        var payload = JsonSafe.Serialize(new BusMessage
        {
            Type = BusServiceTypeEnum.DATA,
            Resource = "Users",
            Action = "Sanctions",
            Caller = caller,
        });

        string response;
        try
        {
            response = await rpc.CallAsync(PlatformQueues.Default, payload, ct);
        }
        catch (RpcException ex) when (ex.Code == "UNAUTHORIZED")
        {
            // No Platform account behind this Keycloak subject: nothing to enforce.
            return CallerSanctions.None;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.Error(ex, "Could not read Platform sanctions; refusing the publication.");
            throw new InternalServerErrorException(
                "SANCTIONS_UNAVAILABLE",
                "Moderation status is temporarily unavailable, please retry");
        }

        return JsonSafe.Deserialize<CallerSanctions>(response) ?? CallerSanctions.None;
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

    private sealed record CacheEntry(CallerSanctions Sanctions, DateTime ExpiresAt);

    private sealed class CallerSanctions
    {
        public static readonly CallerSanctions None = new();

        public bool Banned { get; init; }

        public ActiveMute? ActiveMute { get; init; }
    }

    private sealed class ActiveMute
    {
        public string Reason { get; init; } = "";

        public DateTime EndDate { get; init; }
    }
}
