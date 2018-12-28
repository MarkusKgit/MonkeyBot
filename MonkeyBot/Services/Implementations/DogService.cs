using dokas.FluentStrings;
using MonkeyBot.Services.Common.Dog;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace MonkeyBot.Services
{
    public class DogService : IDogService
    {
        public async Task<string> GetDogPictureUrlAsync()
        {
            using (var httpClient = new HttpClient())
            {
                var json = await httpClient.GetStringAsync($"https://dog.ceo/api/breeds/image/random");

                if (!json.IsEmpty())
                {
                    var dogResponse = JsonConvert.DeserializeObject<DogResponse>(json);
                    if (dogResponse.Status == "success" && dogResponse.Message != null)
                        return dogResponse.Message;
                }
                return string.Empty;
            }
        }

        public async Task<string> GetDogPictureUrlAsync(string breed)
        {
            using (var httpClient = new HttpClient())
            {
                var json = await httpClient.GetStringAsync($"https://dog.ceo/api/breed/{breed}/images/random");

                if (!json.IsEmpty())
                {
                    var dogResponse = JsonConvert.DeserializeObject<DogResponse>(json);
                    if (dogResponse.Status == "success" && dogResponse.Message != null)
                        return dogResponse.Message;
                }
                return string.Empty;
            }
        }

        public async Task<List<string>> GetDogBreedsAsync()
        {
            using (var httpClient = new HttpClient())
            {
                var json = await httpClient.GetStringAsync($"https://dog.ceo/api/breeds/list/all");

                if (!json.IsEmpty())
                {
                    var dogResponse = JsonConvert.DeserializeObject<DogBreedsResponse>(json);
                    if (dogResponse.Status == "success" && dogResponse.Message != null)
                    {
                        return dogResponse.Message.Keys.ToList();
                    }
                }
                return Enumerable.Empty<string>().ToList();
            }
        }
    }
}