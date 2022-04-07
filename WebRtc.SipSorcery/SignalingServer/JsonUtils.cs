using System.Text.Json;

namespace SignalingServer
{
    public static class JsonUtils
    {
        public static T? Deserialize<T>(string json)
        {
            return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }

        public static string Serialize<T>(T value)
        {
            return JsonSerializer.Serialize<T>(value, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }
    }
}