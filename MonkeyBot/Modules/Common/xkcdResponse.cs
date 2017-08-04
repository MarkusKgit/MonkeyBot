using Newtonsoft.Json;

namespace MonkeyBot.Modules.Common
{
    public class xkcdResponse
    {
        [JsonProperty("num")]
        public int Number { get; set; }

        [JsonProperty("day")]
        public string Day { get; set; }

        [JsonProperty("month")]
        public string Month { get; set; }

        [JsonProperty("year")]
        public string Year { get; set; }

        [JsonProperty("safe_title")]
        public string Title { get; set; }

        [JsonProperty("img")]
        public string ImgUrl { get; set; }

        [JsonProperty("transcript")]
        public string Transcript { get; set; }

        [JsonProperty("alt")]
        public string Alt { get; set; }

        [JsonProperty("link")]
        public string Link { get; set; }

        [JsonProperty("news")]
        public string News { get; set; }
    }
}