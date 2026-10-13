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
