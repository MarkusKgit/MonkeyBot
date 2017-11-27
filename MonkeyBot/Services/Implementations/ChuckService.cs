using MonkeyBot.Common;
using MonkeyBot.Services.Common.Chuck;
using Newtonsoft.Json;
using System.Net.Http;
using System.Threading.Tasks;

namespace MonkeyBot.Services
{
    public class ChuckService : IChuckService
    {
        public async Task<string> GetChuckFactAsync()
        {
            using (var httpClient = new HttpClient())
            {
                var json = await httpClient.GetStringAsync($"http://api.icndb.com/jokes/random");

                if (!string.IsNullOrEmpty(json))
                {
                    var chuckResponse = JsonConvert.DeserializeObject<ChuckResponse>(json);
                    if (chuckResponse.Type == "success" && chuckResponse.Value != null)
                        return Helpers.CleanHtmlString(chuckResponse.Value.Joke);
                }
                return string.Empty;
            }
        }

        public async Task<string> GetChuckFactAsync(string userName)
        {
            if (string.IsNullOrEmpty(userName))
                return string.Empty;
            using (var httpClient = new HttpClient())
            {
                var json = await httpClient.GetStringAsync($"http://api.icndb.com/jokes/random?firstName={userName}");

                if (!string.IsNullOrEmpty(json))
                {
                    var chuckResponse = JsonConvert.DeserializeObject<ChuckResponse>(json);
                    if (chuckResponse.Type == "success" && chuckResponse.Value != null)
                        return Helpers.CleanHtmlString(chuckResponse.Value.Joke.Replace("Norris", "").Replace("  ", " "));
                }
                return string.Empty;
            }
        }
    }
}