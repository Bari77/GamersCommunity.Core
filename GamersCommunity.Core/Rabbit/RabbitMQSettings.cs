namespace GamersCommunity.Core.Rabbit
{
    /// <summary>
    /// Strongly-typed RabbitMQ connection settings used by producers/consumers.
    /// </summary>
    /// <remarks>
    /// Bind this type from the <c>RabbitMQ</c> configuration section via the Options pattern.
    /// All properties are <see langword="required"/> and should be validated at startup.
    /// Avoid storing secrets in source control; prefer user secrets, environment variables, or a secret manager.
    /// </remarks>
    /// <example>
    /// Example <c>appsettings.json</c>:
    /// <code>
    /// {
    ///   "RabbitMQ": {
    ///     "Hostname": "localhost",
    ///     "Username": "guest",
    ///     "Password": "guest"
    ///   }
    /// }
    /// </code>
    /// </example>
    public class RabbitMQSettings
    {
        /// <summary>
        /// DNS name or IP address of the RabbitMQ broker (e.g., <c>"localhost"</c> or <c>"rabbitmq.internal"</c>).
        /// </summary>
        public required string Hostname { get; set; }

        /// <summary>
        /// Username used to authenticate with the RabbitMQ broker.
        /// </summary>
        public required string Username { get; set; }

        /// <summary>
        /// Password used to authenticate with the RabbitMQ broker.
        /// Store securely (user secrets, environment variables, or a secret manager).
        /// </summary>
        public required string Password { get; set; }
    }
}
