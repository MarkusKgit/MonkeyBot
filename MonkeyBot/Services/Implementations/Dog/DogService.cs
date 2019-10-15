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
        
        public async Task<string> GetDogPictureUrlAsync(string breed = "")
        {
            Uri apiUri = string.IsNullOrEmpty(breed) ? randomPictureUri : GetRandomPictureForBreedUri(breed);

            HttpClient httpClient = clientFactory.CreateClient();
            string json = await httpClient.GetStringAsync(apiUri).ConfigureAwait(false);

            if (!json.IsEmpty())
            {
                DogResponse dogResponse = JsonSerializer.Deserialize<DogResponse>(json);
                if (dogResponse.Status == "success" && dogResponse.Message != null)
                {
                    return dogResponse.Message;
                }
            }
            return string.Empty;
        }

        public async Task<List<string>> GetDogBreedsAsync()
        {
            HttpClient httpClient = clientFactory.CreateClient();
            string json = await httpClient.GetStringAsync(breedsUri).ConfigureAwait(false);

            if (!json.IsEmpty())
            {
                DogBreedsResponse dogResponse = JsonSerializer.Deserialize<DogBreedsResponse>(json);
                if (dogResponse.Status == "success" && dogResponse.Message != null)
                {
                    return dogResponse.Message.Keys.ToList();
                }
            }
            return Enumerable.Empty<string>().ToList();
        }
    }
}