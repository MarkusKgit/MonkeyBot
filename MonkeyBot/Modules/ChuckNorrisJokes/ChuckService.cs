using MonkeyBot.Common;
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace MonkeyBot.Services
{
    //http://www.icndb.com/api/
    public class ChuckService : IChuckService
    {
        private readonly IHttpClientFactory clientFactory;

        private static readonly Uri randomJokeApiUrl = new Uri("http://api.icndb.com/jokes/random");

        public ChuckService(IHttpClientFactory clientFactory)
        {
            this.clientFactory = clientFactory;
        }

        public Task<string> GetChuckFactAsync()
            => GetJokeAsync(randomJokeApiUrl);

        public Task<string> GetChuckFactAsync(string userName)
            => userName.IsEmpty()
                ? Task.FromResult(string.Empty)
                : GetJokeAsync(new UriBuilder(randomJokeApiUrl) { Query = $"firstName={userName}" }.Uri);


        private async Task<string> GetJokeAsync(Uri uri)
        {
            HttpClient httpClient = clientFactory.CreateClient();
            string json = await httpClient.GetStringAsync(uri);
            if (!json.IsEmpty())
            {
                ChuckResponse chuckResponse = JsonSerializer.Deserialize<ChuckResponse>(json);
                if (chuckResponse.Type == "success" && chuckResponse.Value != null)
                {
                    return MonkeyHelpers.CleanHtmlString(chuckResponse.Value.Joke);
                }
            }
            return string.Empty;
        }
    }
}