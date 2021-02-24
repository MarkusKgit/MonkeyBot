using Humanizer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace MonkeyBot.Services
{
    // https://dog.ceo/dog-api/documentation/
    public class DogService : IDogService
    {
        private readonly IHttpClientFactory clientFactory;

        private static readonly Uri baseApiUri = new Uri("https://dog.ceo/api/");
        private static readonly Uri randomPictureUri = new Uri(baseApiUri, "breeds/image/random");
        private static Uri GetRandomPictureForBreedUri(string breed) => new Uri(baseApiUri, $"breed/{breed}/images/random");
        private static readonly Uri breedsUri = new Uri(baseApiUri, "breeds/list/all");

        public DogService(IHttpClientFactory clientFactory)
        {
            this.clientFactory = clientFactory;
        }
        
        public async Task<Uri> GetRandomPictureUrlAsync(string breed = "")
        {
            Uri apiUri = string.IsNullOrEmpty(breed) ? randomPictureUri : GetRandomPictureForBreedUri(breed);

            HttpClient httpClient = clientFactory.CreateClient();
            string json = await httpClient.GetStringAsync(apiUri);

            if (!json.IsEmpty())
            {
                DogResponse dogResponse = JsonSerializer.Deserialize<DogResponse>(json);
                if (dogResponse.Status == "success" && dogResponse.Message != null)
                {
                    return new Uri(dogResponse.Message);
                }
            }
            return null;
        }

        public async Task<List<string>> GetBreedsAsync()
        {
            HttpClient httpClient = clientFactory.CreateClient();
            string json = await httpClient.GetStringAsync(breedsUri);

            if (!json.IsEmpty())
            {
                DogBreedsResponse dogResponse = JsonSerializer.Deserialize<DogBreedsResponse>(json);
                if (dogResponse.Status == "success" && dogResponse.Message != null)
                {
                    return dogResponse.Message.Keys.Select(breed => breed.Pascalize()).ToList();
                }
            }
            return Enumerable.Empty<string>().ToList();
        }
    }
}