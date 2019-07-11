using Newtonsoft.Json;
using System.Collections.Generic;

namespace MonkeyBot.Services
{
    public class DogBreedsResponse
    {
        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("message")]
        public Dictionary<string, List<string>> Message { get; set; }
    }
}