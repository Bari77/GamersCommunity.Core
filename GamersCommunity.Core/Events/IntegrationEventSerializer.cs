using System.Text.Json;
using System.Text.Json.Serialization;

namespace GamersCommunity.Core.Events
{
    /// <summary>
    /// Wire format shared by integration event publishers and subscribers.
    /// </summary>
    /// <remarks>
    /// camelCase JSON keeps the payloads readable and consumable by non-.NET subscribers.
    /// </remarks>
    public static class IntegrationEventSerializer
    {
        private static readonly JsonSerializerOptions Options = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        public static string Serialize<T>(T value) => JsonSerializer.Serialize(value, Options);

        public static T? Deserialize<T>(string json) => JsonSerializer.Deserialize<T>(json, Options);

        /// <summary>
        /// Reads the <c>type</c> discriminator without deserializing the whole payload.
        /// </summary>
        public static string? ReadType(string json)
        {
            try
            {
                using var document = JsonDocument.Parse(json);
                return document.RootElement.TryGetProperty("type", out var type) && type.ValueKind == JsonValueKind.String
                    ? type.GetString()
                    : null;
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }
}
