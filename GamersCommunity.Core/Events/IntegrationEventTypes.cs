namespace GamersCommunity.Core.Events;

/// <summary>
/// Discriminator values carried by the <c>type</c> property of every integration event.
/// </summary>
public static class IntegrationEventTypes
{
    public const string UserIdentityChanged = "user.identity.changed";
}
