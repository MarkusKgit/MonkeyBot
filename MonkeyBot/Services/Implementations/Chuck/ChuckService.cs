using MonkeyBot.Common;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace MonkeyBot.Services
{
    //http://www.icndb.com/api/
    public class ChuckService : IChuckService
    {
        private static readonly Uri randomJokeApiUrl = new Uri("http://api.icndb.com/jokes/random");

        public async Task<string> GetChuckFactAsync()
        {
            using var httpClient = new HttpClient();
            string json = await httpClient.GetStringAsync(randomJokeApiUrl).ConfigureAwait(false);

            if (!json.IsEmpty())
            {
                ChuckResponse chuckResponse = JsonConvert.DeserializeObject<ChuckResponse>(json);
                if (chuckResponse.Type == "success" && chuckResponse.Value != null)
                    return MonkeyHelpers.CleanHtmlString(chuckResponse.Value.Joke);
            }
            return string.Empty;
        }

        public async Task<string> GetChuckFactAsync(string userName)
        {
            if (userName.IsEmpty())
                return string.Empty;
            using var httpClient = new HttpClient();
            var url = new UriBuilder(randomJokeApiUrl)
            {
                Query = $"firstName={userName}"
            };
            string json = await httpClient.GetStringAsync(url.Uri).ConfigureAwait(false);

            if (!json.IsEmpty())
            {
                ChuckResponse chuckResponse = JsonConvert.DeserializeObject<ChuckResponse>(json);
                if (chuckResponse.Type == "success" && chuckResponse.Value != null)
                    return MonkeyHelpers.CleanHtmlString(chuckResponse.Value.Joke.Replace("Norris", "").Replace("  ", " "));
            }
            return string.Empty;
        }
    }
}