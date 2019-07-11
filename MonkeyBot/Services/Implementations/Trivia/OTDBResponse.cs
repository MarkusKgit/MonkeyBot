using Newtonsoft.Json;
using System.Collections.Generic;

namespace MonkeyBot.Services
{
    //https://opentdb.com/api_config.php

    public class OTDBResponse
    {
        [JsonProperty(PropertyName = "response_code")]
        public TriviaApiResponse Response { get; set; }

        [JsonProperty(PropertyName = "results")]
        public List<OTDBQuestion> Questions { get; set; }
    }

    public enum TriviaApiResponse
    {
        Success = 0,
        NoResults = 1,
        InvalidParameter = 2,
        TokenNotFound = 3,
        TokenEmpty = 4
    }
}