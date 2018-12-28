using Newtonsoft.Json;

namespace MonkeyBot.Services.Common.Dog
{
    // https://dog.ceo/dog-api/documentation/
    public class DogResponse
    {
        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }
    }
}