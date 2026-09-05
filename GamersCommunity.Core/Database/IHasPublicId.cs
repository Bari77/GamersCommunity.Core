namespace GamersCommunity.Core.Database;

/// <summary>
/// Optional public identifier for entities exposed outside the service boundary.
/// </summary>
public interface IHasPublicId
{
    Guid PublicId { get; set; }
}
