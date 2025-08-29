using GamersCommunity.Core.Exceptions;
using GamersCommunity.Core.Services;

namespace GamersCommunity.Core.Rabbit
{
    /// <summary>
    /// Routes incoming <see cref="RabbitMQTableMessage"/> instances to the matching
    /// <see cref="ITableService"/> based on the message <c>Table</c> value.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The router scans the injected <see cref="ITableService"/> implementations and selects
    /// the first whose <c>TableName</c> matches (case-insensitive) the message's <c>Table</c>.
    /// It then forwards the call to <see cref="ITableService.HandleAsync(string, string?, int?, System.Threading.CancellationToken)"/>.
    /// </para>
    /// <para>
    /// Register all table services in DI (e.g., <c>services.AddSingleton<ITableService, UsersService>()</c>)
    /// so they can be discovered by the router.
    /// </para>
    /// </remarks>
    /// <param name="services">The collection of table services available for routing.</param>
    /// <example>
    /// <code>
    /// // Service registration
    /// services.AddSingleton<ITableService, UsersService>();
    /// services.AddSingleton<ITableService, ClassesService>();
    /// services.AddSingleton<TableRouter>();
    ///
    /// // Example ITableService
    /// public sealed class UsersService : ITableService
    /// {
    ///     public string TableName => "Users";
    ///
    ///     public Task<string> HandleAsync(string action, string? data, int? id, CancellationToken ct)
    ///     {
    ///         // Implement your CRUD/custom actions here...
    ///         return Task.FromResult("ok");
    ///     }
    /// }
    /// </code>
    /// </example>
    public class TableRouter(IEnumerable<ITableService> services)
    {
        private readonly IEnumerable<ITableService> _services = services;

        /// <summary>
        /// Routes the specified <paramref name="tableMessage"/> to the appropriate table service
        /// and returns the service response.
        /// </summary>
        /// <param name="tableMessage">The message containing the target table, action, data, and optional id.</param>
        /// <param name="ct">A cancellation token to observe while waiting for the task to complete.</param>
        /// <returns>A <see cref="string"/> produced by the target <see cref="ITableService"/>.</returns>
        /// <exception cref="NotFoundException">
        /// Thrown when no <see cref="ITableService"/> is registered for the table specified in <paramref name="tableMessage"/>.
        /// </exception>
        public async Task<string> RouteAsync(RabbitMQTableMessage tableMessage, CancellationToken ct = default)
        {
            var service = _services.FirstOrDefault(
                s => s.TableName.Equals(tableMessage.Table, StringComparison.OrdinalIgnoreCase));

            return service == null
                ? throw new NotFoundException($"No service found for table {tableMessage.Table}")
                : await service.HandleAsync(tableMessage.Action, tableMessage.Data, tableMessage.Id, ct);
        }
    }
}