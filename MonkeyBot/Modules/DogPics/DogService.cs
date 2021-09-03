using Humanizer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace MonkeyBot.Services
{
    // https://dog.ceo/dog-api/documentation/
    public class DogService : IDogService
    {
        private readonly IHttpClientFactory _clientFactory;

        private static readonly Uri baseApiUri = new("https://dog.ceo/api/");
        private static readonly Uri randomPictureUri = new(baseApiUri, "breeds/image/random");
        private static Uri GetRandomPictureForBreedUri(string breed) => new(baseApiUri, $"breed/{breed}/images/random");
        private static readonly Uri breedsUri = new(baseApiUri, "breeds/list/all");

        public DogService(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        public async Task<Uri> GetRandomPictureUrlAsync(string breed = "")
        {
            Uri apiUri = string.IsNullOrEmpty(breed) ? randomPictureUri : GetRandomPictureForBreedUri(breed);

            var httpClient = _clientFactory.CreateClient();
            try
            {
                var dogResponse = await httpClient.GetFromJsonAsync<DogResponse>(apiUri);
                if (dogResponse.Status == "success" && dogResponse.Message != null)
                {
                    return new Uri(dogResponse.Message);
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
                var dogBreedsResponse = await httpClient.GetFromJsonAsync<DogBreedsResponse>(breedsUri);
                if (dogBreedsResponse.Status == "success" && dogBreedsResponse.Message != null)
                {
                    return dogBreedsResponse.Message.Keys.Select(breed => breed.Pascalize()).ToList();
                }
            }
            catch { }
            return new List<string>();
        }
    }

    public record DogResponse(string Status, string Message) { }
    public record DogBreedsResponse(string Status, Dictionary<string, List<string>> Message) { }
}