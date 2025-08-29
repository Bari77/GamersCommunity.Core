namespace GamersCommunity.Core.Rabbit
{
    /// <summary>
    /// Message envelope used to communicate between the API gateway and worker services over RabbitMQ.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This DTO is intentionally minimal: it carries the target <see cref="Table"/>, the requested
    /// <see cref="Action"/>, an optional resource <see cref="Id"/>, and an opaque <see cref="Data"/> payload.
    /// The payload is commonly a JSON string that the target table service knows how to parse.
    /// </para>
    /// <para>
    /// Instances of this type are produced by the gateway and consumed by workers (see
    /// <c>BasicServiceConsumer</c> and <c>TableRouter</c>). Validation of required fields is expected to be
    /// performed at the edges (gateway or consumer) before processing.
    /// </para>
    /// </remarks>
    /// <example>
    /// Example JSON payload sent via RabbitMQ:
    /// <code>
    /// {
    ///   "table": "Users",
    ///   "action": "Update",
    ///   "id": 42,
    ///   "data": "{\"email\":\"new@example.com\",\"displayName\":\"Alice\"}"
    /// }
    /// </code>
    /// </example>
    public class RabbitMQTableMessage
    {
        /// <summary>
        /// Logical table or domain name targeted by the request (e.g., <c>"Users"</c>, <c>"Classes"</c>).
        /// </summary>
        /// <remarks>
        /// The router selects the appropriate <c>ITableService</c> by matching this value (case-insensitive).
        /// </remarks>
        public string Table { get; set; } = string.Empty;

        /// <summary>
        /// Operation to perform on the target table.
        /// </summary>
        /// <remarks>
        /// May be a CRUD verb (e.g., <c>"Create"</c>, <c>"Get"</c>, <c>"List"</c>, <c>"Update"</c>, <c>"Delete"</c>)
        /// or any custom action agreed upon by the producer and the table service (e.g., <c>"BulkUpsert"</c>,
        /// <c>"ExportCsv"</c>).
        /// </remarks>
        public string Action { get; set; } = string.Empty;

        /// <summary>
        /// Opaque payload for the action, typically a JSON string.
        /// </summary>
        /// <remarks>
        /// The format and schema are defined by the target table service for the specified <see cref="Action"/>.
        /// When unused (e.g., simple <c>Get</c>/<c>Delete</c> by <see cref="Id"/>), this may be empty.
        /// </remarks>
        public string Data { get; set; } = string.Empty;

        /// <summary>
        /// Optional resource identifier associated with the action (e.g., primary key for <c>Get</c>/<c>Update</c>/<c>Delete</c>).
        /// </summary>
        /// <remarks>
        /// When not applicable (e.g., <c>Create</c> or list operations), this value can be <see langword="null"/>.
        /// </remarks>
        public int? Id { get; set; }
    }
}
