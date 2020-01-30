using Microsoft.Extensions.Logging;
using MonkeyBot.Common;
using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;

namespace MonkeyBot.Services
{
    public class PixabayService : IPictureSearchService
    {
        private readonly IHttpClientFactory clientFactory;
        private readonly ILogger<PixabayService> logger;
        private readonly string apiKey;
        private const int hitsPerPage = 10;
        private static readonly Random rng = new Random();

        private static readonly Uri baseApiUri = new Uri("https://pixabay.com/api/");
        private Uri GetUriForSearchTerm(string searchTerm, int page = 1)
            => new Uri(baseApiUri, $"?key={apiKey}&q={HttpUtility.UrlEncode(searchTerm)}&image_type=photo&safesearch=true&per_page={hitsPerPage}&page={page}");

        public PixabayService(IHttpClientFactory clientFactory, ILogger<PixabayService> logger)
        {
            this.clientFactory = clientFactory;
            this.logger = logger;

            DiscordClientConfiguration config = DiscordClientConfiguration.LoadAsync().GetAwaiter().GetResult();

            if (config.PixabayApiKey.IsEmptyOrWhiteSpace())
            {
                throw new NotSupportedException("Pixabay Api key must be provided in the config file!");
            }
            apiKey = config.PixabayApiKey;
        }

        public async Task<string> GetRandomPictureUrlAsync(string searchterm)
        {
            // First get the results for the search term to determine the total amount of hits
            HttpClient httpClient = clientFactory.CreateClient();
            string json = await httpClient.GetStringAsync(GetUriForSearchTerm(searchterm)).ConfigureAwait(false);

            if (json.IsEmpty())
            {
                return "";
            }

            PixabayResponse response = JsonSerializer.Deserialize<PixabayResponse>(json);
            if (response == null || response.TotalHits < 1)
            {
                return "";
            }

            // Get a random picture number based on the amount of total hits. 
            // We then have to find the correct page and offset
            // Only use the top results for meaninful results
            int randomPictureNr = rng.Next(1, (int)(0.3 * response.TotalHits));
            // Page is starting at 1
            int page = 1 + randomPictureNr / hitsPerPage;
            // Picture Index is 0 based
            int pictureIndex = (randomPictureNr % hitsPerPage) - 1;

            // Get the result from the actual page
            json = await httpClient.GetStringAsync(GetUriForSearchTerm(searchterm, page)).ConfigureAwait(false);

            if (json.IsEmpty())
            {
                return "";
            }

            response = JsonSerializer.Deserialize<PixabayResponse>(json);
            return response?.Hits?.ElementAtOrDefault(pictureIndex)?.WebformatURL ?? "";
        }
    }
}
