using Newtonsoft.Json;
using System;

namespace MonkeyBot.Services
{
    public class XkcdResponse
    {
        [JsonProperty("month")]
        public string Month { get; set; }

        [JsonProperty("num")]
        public int Number { get; set; }

        [JsonProperty("link")]
        public string Link { get; set; }

        [JsonProperty("year")]
        public string Year { get; set; }

        [JsonProperty("news")]
        public string News { get; set; }

        [JsonProperty("safe_title")]
        public string SafeTitle { get; set; }

        [JsonProperty("transcript")]
        public string Transcript { get; set; }

        [JsonProperty("alt")]
        public string Alt { get; set; }

        [JsonProperty("img")]
        public Uri ImgUrl { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("day")]
        public string Day { get; set; }
    }
}