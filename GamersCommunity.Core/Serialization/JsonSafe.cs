using Newtonsoft.Json;

namespace GamersCommunity.Core.Serialization
{
    public static class JsonSafe
    {
        private static readonly JsonSerializerSettings _settings = new()
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore
        };

        public static string Serialize(object value)
            => JsonConvert.SerializeObject(value, _settings);

        public static T? Deserialize<T>(string json)
            => JsonConvert.DeserializeObject<T>(json, _settings);
    }
}
