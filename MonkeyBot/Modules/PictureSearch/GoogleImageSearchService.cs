using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace MonkeyBot.Services
{
    public class GoogleImageSearchService : IPictureSearchService
    {
        private static readonly Random _rng = new();
        private static readonly string[] _imageExtensions = new string[] { "jpg", "jpeg", "png", "gif" };
        private readonly IHttpClientFactory _clientFactory;
        private readonly ILogger<GoogleImageSearchService> _logger;

        public GoogleImageSearchService(IHttpClientFactory clientFactory, ILogger<GoogleImageSearchService> logger)
        {
            _clientFactory = clientFactory;
            _logger = logger;
        }

        public async Task<Uri> GetRandomPictureUrlAsync(string searchterm)
        {
            var web = new HtmlWeb
            {
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/80.0.3987.149 Safari/537.36"
            };
            string url = $"https://www.google.com/search?q={HttpUtility.UrlEncode(searchterm)}&tbm=isch";
            HtmlDocument document = await web.LoadFromWebAsync(url);
            var urls = document.DocumentNode.InnerHtml.Split("[", StringSplitOptions.RemoveEmptyEntries)
                .Select(l => l.Trim('"'))
                .Where(l => l.StartsWith("http", StringComparison.OrdinalIgnoreCase) && _imageExtensions.Contains(l.Split('"')[0].Split('.').Last()))
                .Select(l => l.Split('"')[0])
                .ToList();

            if (urls == null && urls.Count < 1)
            {
                return null;
            }

            for (int i = 0; i < urls.Count; i++)
            {
                string randomImageUrl = urls.ElementAt(_rng.Next(0, urls.Count));
                if (await UrlIsValid(randomImageUrl))
                {
                    return new Uri(randomImageUrl);
                }
                urls.Remove(randomImageUrl);
            }

            return null;
        }

        /// <summary>
        /// This method will check a url to see that it does not return server or protocol errors
        /// </summary>
        /// <param name="url">The path to check</param>
        /// <returns></returns>
        private async Task<bool> UrlIsValid(string url)
        {
            try
            {
                var uri = new Uri(url);                

                using (var result = await Get(uri))
                {
                    int statusCode = (int)result.StatusCode;
                    if (statusCode >= 100 && statusCode < 400) //Good requests
                    {
                        return true;
                    }
                    else if (statusCode >= 500 && statusCode <= 510) //Server Errors
                    {
                        return false;
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"There was a problem checking the url {url}");
            }
            return false;            
        }

        private async Task<HttpResponseMessage> Get(Uri uri)
        {
            var httpClient = _clientFactory.CreateClient();
            var response = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Head, uri));
            return response.StatusCode != HttpStatusCode.MethodNotAllowed
                ? response
                : await httpClient.SendAsync(new HttpRequestMessage() { RequestUri = uri }, HttpCompletionOption.ResponseHeadersRead);
        }
    }
}
