using System.Text.Json.Serialization;

namespace MonkeyBot.Services
{
    public class ChuckResponse
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }
        [JsonPropertyName("value")]
        public ChuckJoke Value { get; set; }
    }
}