using GamersCommunity.Core.Rabbit;

namespace GamersCommunity.Core.Platform;

public interface IPlatformSanctionsClient
{
    /// <summary>
    /// Refuses the call when the caller is muted or banned on Platform.
    /// </summary>
    Task EnsureCanPublishAsync(BusMessage message, CancellationToken ct = default);
}
