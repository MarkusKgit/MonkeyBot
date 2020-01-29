using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;

namespace MonkeyBot.Services
{
    // https://docs.thecatapi.com/
    public class CatService : ICatService
    {
        private readonly IHttpClientFactory clientFactory;

        private static readonly Uri baseApiUri = new Uri("https://api.thecatapi.com/v1/");
        private static Uri GetRandomPictureForBreedUri(string breedId) => new Uri(baseApiUri, $"images/search?size=small&breed_id={breedId}");
        private static readonly Uri breedsUri = new Uri(baseApiUri, "breeds");
        private static Uri SearchBreedUri(string breed) => new Uri(baseApiUri, $"breeds/search?q={HttpUtility.UrlEncode(breed)}");

        public CatService(IHttpClientFactory clientFactory)
        {
            this.clientFactory = clientFactory;
        }

        public async Task<string> GetCatPictureUrlAsync(string breed = "")
        {
            string breedId = "";
            if (!breed.IsEmpty())
            {
                breedId = await GetBreedIdAsync(breed).ConfigureAwait(false);
                if (breedId.IsEmpty())
                {
                    return "";
                }
            }
            HttpClient httpClient = clientFactory.CreateClient();
            string json = await httpClient.GetStringAsync(GetRandomPictureForBreedUri(breedId)).ConfigureAwait(false);

            if (!json.IsEmpty())
            {
                List<CatResponse> cats = JsonSerializer.Deserialize<List<CatResponse>>(json);
                if (cats != null && cats.Count == 1)
                {
                    return cats.First().Url;
                }
            }
            return "";
        }

        public async Task<List<string>> GetCatBreedsAsync()
        {
            HttpClient httpClient = clientFactory.CreateClient();
            string json = await httpClient.GetStringAsync(breedsUri).ConfigureAwait(false);

            if (!json.IsEmpty())
            {
                List<CatBreedsResponse> catBreeds = JsonSerializer.Deserialize<List<CatBreedsResponse>>(json);
                if (catBreeds != null && catBreeds.Count > 0)
                {
                    return catBreeds.Select(x => x.Name).ToList();
                }
            }
            return Enumerable.Empty<string>().ToList();
        }

        private async Task<string> GetBreedIdAsync(string breed)
        {
            if (!breed.IsEmpty())
            {
                HttpClient httpClient = clientFactory.CreateClient();
                string json = await httpClient.GetStringAsync(SearchBreedUri(breed)).ConfigureAwait(false);

                if (!json.IsEmpty())
                {
                    List<CatBreedsResponse> catBreeds = JsonSerializer.Deserialize<List<CatBreedsResponse>>(json);
                    if (catBreeds != null && catBreeds.Count == 1)
                    {
                        return catBreeds.First().Id;
                    }
                }
            }
            return "";
        }
    }
}
