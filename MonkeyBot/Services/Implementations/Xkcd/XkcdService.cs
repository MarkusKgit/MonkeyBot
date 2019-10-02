using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace MonkeyBot.Services
{
    public class XkcdService : IXkcdService
    {
        // https://xkcd.com/~comicNumber~
        // display Url of comic is baseUrl + /comic Number
        // Latest comic json data is baseUrl + /info.0.json
        // Specific comic json data is baseUrl + /comic Number/info.0.json
        private static readonly Uri baseUrl = new Uri("https://xkcd.com/");        
        private static readonly Uri latestComicApiUrl = new Uri(baseUrl, "/info.0.json");
        private Uri GetComicApiUrl(int comicNumber) => new Uri(baseUrl, $"/{comicNumber}/info.0.json");
        public Uri GetComicUrl(int comicNumber) => new Uri(baseUrl, $"/{comicNumber}");

        public async Task<XkcdResponse> GetComicAsync(int number)
        {
            int maxNumer = await GetLatestComicNumberAsync().ConfigureAwait(false);
            if (number < 1 || number > maxNumer || number == 404)
            {
                throw new ArgumentOutOfRangeException(nameof(number), "The specified comic does not exist!");
            }
            return await GetComicAsync(GetComicApiUrl(number)).ConfigureAwait(false);
        }

        public async Task<XkcdResponse> GetLatestComicAsync() => await GetComicAsync(latestComicApiUrl).ConfigureAwait(false);

        public async Task<XkcdResponse> GetRandomComicAsync()
        {
            int max = await GetLatestComicNumberAsync().ConfigureAwait(false);
            var rnd = new Random();
            int rndNumber;
            while ((rndNumber = rnd.Next(1, max)) == 404) { } // xkcd 404 does not exist  
            return await GetComicAsync(rndNumber).ConfigureAwait(false);
        }

        private async Task<int> GetLatestComicNumberAsync() => (await GetLatestComicAsync().ConfigureAwait(false))?.Number ?? 0;

        private async Task<XkcdResponse> GetComicAsync(Uri apiEndPoint)
        {
            using (var http = new HttpClient())
            {                
                try
                {
                    string response = await http.GetStringAsync(apiEndPoint).ConfigureAwait(false);
                    return JsonConvert.DeserializeObject<XkcdResponse>(response);
                }
                catch
                {
                    return null;
                }
            }
        }
    }
}
