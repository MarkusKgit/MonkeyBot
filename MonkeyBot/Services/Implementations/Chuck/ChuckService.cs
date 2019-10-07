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

        public Task<string> GetChuckFactAsync() 
            => GetJokeAsync(randomJokeApiUrl);

        public Task<string> GetChuckFactAsync(string userName)        
            => userName.IsEmpty()
                ? Task.FromResult(string.Empty)
                : GetJokeAsync(new UriBuilder(randomJokeApiUrl) { Query = $"firstName={userName}" }.Uri);
        

        private static async Task<string> GetJokeAsync(Uri uri)
        {
            using var httpClient = new HttpClient();
            string json = await httpClient.GetStringAsync(uri).ConfigureAwait(false);
            if (!json.IsEmpty())
            {
                ChuckResponse chuckResponse = JsonConvert.DeserializeObject<ChuckResponse>(json);
                if (chuckResponse.Type == "success" && chuckResponse.Value != null)
                {
                    return MonkeyHelpers.CleanHtmlString(chuckResponse.Value.Joke);
                }
            }
            return string.Empty;
        }
    }
}