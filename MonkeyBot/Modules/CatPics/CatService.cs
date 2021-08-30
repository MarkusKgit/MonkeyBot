using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Web;

namespace MonkeyBot.Services
{
    // https://docs.thecatapi.com/
    public class CatService : ICatService
    {
        private readonly IHttpClientFactory _clientFactory;

        private static readonly Uri baseApiUri = new("https://api.thecatapi.com/v1/");
        private static Uri GetRandomPictureForBreedUri(string breedId) => new Uri(baseApiUri, $"images/search?size=small&breed_id={breedId}");
        private static readonly Uri breedsUri = new(baseApiUri, "breeds");
        private static Uri SearchBreedUri(string breed) => new(baseApiUri, $"breeds/search?q={HttpUtility.UrlEncode(breed)}");

        public CatService(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        public async Task<Uri> GetRandomPictureUrlAsync(string breed = "")
        {
            string breedId = "";
            if (!breed.IsEmpty())
            {
                breedId = await GetBreedIdAsync(breed);
                if (breedId.IsEmpty())
                {
                    return null;
                }
            }
            var httpClient = _clientFactory.CreateClient();
            try
            {
                var cats = await httpClient.GetFromJsonAsync<List<CatResponse>>(GetRandomPictureForBreedUri(breedId));
                if (cats != null && cats.Count == 1)
                {
                    return cats.First().Url;
                }
            }
            catch { }
            return null;
        }

        public async Task<List<string>> GetBreedsAsync()
        {
            var httpClient = _clientFactory.CreateClient();
            try
            {
                var catBreeds = await httpClient.GetFromJsonAsync<List<CatBreedsResponse>>(breedsUri);
                return catBreeds.Select(x => x.Name).ToList();
            }
            catch
            {
                return new List<string>();
            }
        }

        private async Task<string> GetBreedIdAsync(string breed)
        {
            var httpClient = _clientFactory.CreateClient();
            try
            {
                var catBreeds = await httpClient.GetFromJsonAsync<List<CatBreedsResponse>>(SearchBreedUri(breed));
                if (catBreeds != null && catBreeds.Count == 1)
                {
                    return catBreeds.First().Id;
                }
            }
            catch { }
            return "";
        }
    }

    public record CatResponse(Uri Url) { }
    public record CatBreedsResponse(string Id, string Name) { }
}
