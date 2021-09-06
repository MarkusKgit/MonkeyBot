using HtmlAgilityPack;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;

namespace MonkeyBot.Services
{
    public class GoogleImageSearchService : IPictureSearchService
    {
        private static readonly Random _rng = new();
        private static readonly string[] _imageExtensions = new string[] { "jpg", "jpeg", "png", "gif" };

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
                if (UrlIsValid(randomImageUrl))
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
        private static bool UrlIsValid(string url)
        {
            try
            {
                var request = WebRequest.Create(new Uri(url)) as HttpWebRequest;
                request.Timeout = 1000;
                request.Method = "HEAD"; //Get only the header information -- no need to download any content

                using HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                int statusCode = (int)response.StatusCode;
                if (statusCode >= 100 && statusCode < 400) //Good requests
                {
                    return true;
                }
                else if (statusCode >= 500 && statusCode <= 510) //Server Errors
                {
                    return false;
                }
            }
            catch (WebException ex)
            {
                if (ex.Status == WebExceptionStatus.ProtocolError) //400 errors
                {
                    return false;
                }
            }
            catch (Exception) { } //YOLO
            return false;
        }
    }
}
