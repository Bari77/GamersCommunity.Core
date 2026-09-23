using GamersCommunity.Core.Rabbit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Serilog;

namespace GamersCommunity.Core.Platform;

/// <summary>
/// DI helpers for shared Platform RPC clients used by game Consumers.
/// </summary>
public static class PlatformRpcServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IRabbitRpcClient"/> (without active-consumer gate) and the Platform
    /// sanctions / conversations / friends clients.
    /// </summary>
    public static IServiceCollection AddPlatformRpcClients(this IServiceCollection services)
    {
        services.TryAddSingleton<IRabbitRpcClient>(sp =>
            new RabbitRpcClient(
                sp.GetRequiredService<IOptions<RabbitMQSettings>>(),
                sp.GetRequiredService<ILogger>(),
                new RabbitRpcClientOptions { RequireActiveConsumer = false }));

        services.TryAddSingleton<IPlatformSanctionsClient, PlatformSanctionsClient>();
        services.TryAddSingleton<IPlatformConversationsClient, PlatformConversationsClient>();
        services.TryAddSingleton<IPlatformFriendsClient, PlatformFriendsClient>();
        return services;
    }
}
