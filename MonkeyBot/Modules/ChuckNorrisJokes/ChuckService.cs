using MonkeyBot.Common;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace MonkeyBot.Services
{
    //http://www.icndb.com/api/
    public class ChuckService : IChuckService
    {
        private readonly IHttpClientFactory _clientFactory;

        private static readonly Uri randomJokeApiUrl = new Uri("http://api.icndb.com/jokes/random");

        public ChuckService(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        public Task<string> GetChuckFactAsync()
            => GetJokeAsync(randomJokeApiUrl);

        public Task<string> GetChuckFactAsync(string userName)
            => userName.IsEmpty()
                ? Task.FromResult(string.Empty)
                : GetJokeAsync(new UriBuilder(randomJokeApiUrl) { Query = $"firstName={userName}" }.Uri);


        private async Task<string> GetJokeAsync(Uri uri)
        {
            HttpClient httpClient = _clientFactory.CreateClient();
            try
            {
                var chuckResponse = await httpClient.GetFromJsonAsync<ChuckResponse>(uri);
                if (chuckResponse.Type == "success" && chuckResponse.Value != null)
                {
                    return MonkeyHelpers.CleanHtmlString(chuckResponse.Value.Joke);
                }
            }
            catch { }
            return string.Empty;
        }
    }

    public record ChuckResponse(string Type, ChuckJoke Value) { }
    public record ChuckJoke(string Joke) { }
}