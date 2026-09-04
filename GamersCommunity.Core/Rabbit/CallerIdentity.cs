namespace GamersCommunity.Core.Rabbit
{
    public sealed class CallerIdentity
    {
        public string? Subject { get; init; }
        public string? Email { get; init; }
        public string? Username { get; init; }
        public string[] Roles { get; init; } = [];
    }
}
