using GamersCommunity.Core.Exceptions;
using GamersCommunity.Core.Services;

namespace GamersCommunity.Core.Rabbit
{
    public class TableRouter(IEnumerable<ITableService> services)
    {
        private readonly IEnumerable<ITableService> _services = services;

        public async Task<string> RouteAsync(RabbitMQTableMessage tableMessage)
        {
            var service = _services.FirstOrDefault(s => s.TableName.Equals(tableMessage.Table, StringComparison.OrdinalIgnoreCase));
            return service == null
                ? throw new NotFoundException($"No service found for table {tableMessage.Table}")
                : await service.HandleAsync(tableMessage.Action, tableMessage.Data, tableMessage.Id);
        }
    }
}
