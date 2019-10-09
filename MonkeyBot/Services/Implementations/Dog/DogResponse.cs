using System.Text.Json.Serialization;

namespace MonkeyBot.Services
{
    public class DogResponse
    {
        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }
    }
}