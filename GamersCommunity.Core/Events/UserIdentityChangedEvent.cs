namespace GamersCommunity.Core.Events;

/// <summary>
/// Published by the Platform microservice whenever the public identity of a user changes.
/// </summary>
/// <remarks>
/// Carries display data only. Roles, sanctions and any other authorization state are deliberately
/// excluded: they must stay at their source of truth and never be read from a replicated copy.
/// </remarks>
public sealed record UserIdentityChangedEvent : IntegrationEvent
{
    public override string Type => IntegrationEventTypes.UserIdentityChanged;

    public Guid UserPublicId { get; init; }

    public string Nickname { get; init; } = string.Empty;

    public string Discriminator { get; init; } = string.Empty;

    public string AvatarUrl { get; init; } = string.Empty;
}
