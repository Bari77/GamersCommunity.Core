namespace GamersCommunity.Core.Rabbit
{
    public class RabbitMQTableMessage
    {
        public string Table { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string Data { get; set; } = string.Empty;
        public int? Id { get; set; }
    }
}
