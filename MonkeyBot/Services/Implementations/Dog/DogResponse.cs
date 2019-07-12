using Newtonsoft.Json;

namespace MonkeyBot.Services
{    
    public class DogResponse
    {
        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }
    }
}