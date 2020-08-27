using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MonkeyBot.Services
{
    public class ChuckJoke
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
        [JsonPropertyName("joke")]
        public string Joke { get; set; }
        [JsonPropertyName("categories")]
        public List<string> Categories { get; set; }
    }
}