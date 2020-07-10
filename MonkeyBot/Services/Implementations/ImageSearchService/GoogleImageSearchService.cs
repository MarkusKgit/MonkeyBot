using HtmlAgilityPack;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace MonkeyBot.Services
{
    public class GoogleImageSearchService : IPictureSearchService
    {
        private static Random rnd = new Random();

        public async Task<string> GetRandomPictureUrlAsync(string searchterm)
        {
            var web = new HtmlWeb();
            string url = $"https://www.google.com/search?q={HttpUtility.UrlEncode(searchterm)}&tbm=isch";
            HtmlDocument document = await web.LoadFromWebAsync(url).ConfigureAwait(false);
            var nodes = document.DocumentNode.SelectNodes($"//*[@class='rg_meta notranslate']");
            if (nodes == null || nodes.Count < 1)
                return "";
            var urls = nodes.Select(n => n.InnerHtml)
                            .Where(h => !string.IsNullOrEmpty(h))
                            .Select(n => Regex.Match(n, @"(https?:\/\/.*\.(?:png|jpg|gif))"))
                            .Where(m => m.Success)
                            .Select(m => m.Value)
                            .ToList();

            return (urls != null && urls.Count >= 1) ? urls.ElementAt(rnd.Next(0, urls.Count)) : "";

        }
    }
}
