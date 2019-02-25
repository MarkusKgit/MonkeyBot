using Discord;
using Discord.Commands;
using MonkeyBot.Common;
using MonkeyBot.Modules.Common;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace MonkeyBot.Modules
{
    [Name("Xkcd")]
    public class XkcdModule : MonkeyModuleBase
    {
        private const string comicUrl = "https://xkcd.com/{0}/";
        private const string apiUrlLatest = "https://xkcd.com/info.0.json";
        private const string apiUrlSpecific = "http://xkcd.com/{0}/info.0.json";

        [Command("xkcd")]
        [Remarks("Gets a random xkcd comic or the latest xkcd comic by appending \"latest\" to the command")]
        [Priority(0)]
        [Example("!xkcd latest")]
        [RequireBotPermission(ChannelPermission.EmbedLinks)]
        public async Task GetXkcdAsync(string arg = "")
        {
            xkcdResponse comic = null;
            if (arg.ToLower() == "latest")
            {
                comic = await GetComicAsync(null);
            }
            else
            {
                int max = await GetLatestNumberAsync();
                var rnd = new Random();
                int rndNumber = rnd.Next(1, max);
                while ((rndNumber = rnd.Next(1, max)) == 404) { } // xkcd 404 does not exist                
                comic = await GetComicAsync(rndNumber);
            }
            await EmbedComicAsync(comic, Context.Channel);
        }

        [Command("xkcd")]
        [Remarks("Gets the xkcd comic with the specified number")]
        [Priority(1)]
        [Example("!xkcd 101")]
        [RequireBotPermission(ChannelPermission.EmbedLinks)]
        public async Task GetXkcdAsync(int number)
        {
            int maxNumer = await GetLatestNumberAsync();
            if (number < 1 || number > maxNumer || number == 404)
            {
                await ReplyAsync("The specified comic does not exist!");
                return;
            }
            var comic = await GetComicAsync(number);
            await EmbedComicAsync(comic, Context.Channel);
        }

        private async static Task EmbedComicAsync(xkcdResponse comic, IMessageChannel channel)
        {
            if (comic == null)
                return;
            var builder = new EmbedBuilder();
            builder.WithImageUrl(comic.ImgUrl);
            builder.WithAuthor($"xkcd #{comic.Number}", "https://xkcd.com/s/919f27.ico", string.Format(comicUrl, comic.Number));
            builder.WithTitle(comic.Title);
            builder.WithDescription(comic.Alt);
            if (int.TryParse(comic.Year, out int year) && int.TryParse(comic.Month, out int month) && int.TryParse(comic.Day, out int day))
            {
                DateTime date = new DateTime(year, month, day);
                builder.WithFooter(date.ToString("yyyy-MM-dd"));
            }
            await channel.SendMessageAsync("", false, builder.Build());
        }

        private async static Task<xkcdResponse> GetComicAsync(int? number)
        {
            using (var http = new HttpClient())
            {
                string apiUrl = number.HasValue ? string.Format(apiUrlSpecific, number.Value) : apiUrlLatest;
                var response = await http.GetStringAsync(apiUrl).ConfigureAwait(false);
                var comic = JsonConvert.DeserializeObject<xkcdResponse>(response);
                return comic;
            }
        }

        private async static Task<int> GetLatestNumberAsync()
        {
            var comic = await GetComicAsync(null);
            if (comic != null)
                return comic.Number;
            return 0;
        }
    }
}