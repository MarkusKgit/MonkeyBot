using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MonkeyBot.Services
{
    public class DogBreedsResponse
    {
        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("message")]
        public Dictionary<string, List<string>> Message { get; set; }
    }
}