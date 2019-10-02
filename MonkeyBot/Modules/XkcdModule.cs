using Discord;
using Discord.Commands;
using Microsoft.Extensions.Logging;
using MonkeyBot.Common;
using MonkeyBot.Services;
using System;
using System.Threading.Tasks;

namespace MonkeyBot.Modules
{
    [Name("Xkcd")]
    public class XkcdModule : MonkeyModuleBase
    {
        private readonly IXkcdService xkcdService;
        private readonly ILogger logger;

        public XkcdModule(IXkcdService xkcdService, ILogger<XkcdModule> logger)
        {
            this.xkcdService = xkcdService;
            this.logger = logger;
        }

        [Command("xkcd")]
        [Remarks("Gets a random xkcd comic or the latest xkcd comic by appending \"latest\" to the command")]
        [Priority(0)]
        [Example("!xkcd latest")]
        [RequireBotPermission(ChannelPermission.EmbedLinks)]
        public async Task GetXkcdAsync(string arg = "")
        {
            XkcdResponse comic;
            if (!string.IsNullOrEmpty(arg) && arg.Equals("latest", StringComparison.OrdinalIgnoreCase))
            {
                comic = await xkcdService.GetLatestComicAsync().ConfigureAwait(false);
            }
            else
            {
                comic = await xkcdService.GetRandomComicAsync().ConfigureAwait(false);
            }
            await EmbedComicAsync(comic, Context.Channel).ConfigureAwait(false);
        }

        [Command("xkcd")]
        [Remarks("Gets the xkcd comic with the specified number")]
        [Priority(1)]
        [Example("!xkcd 101")]
        [RequireBotPermission(ChannelPermission.EmbedLinks)]
        public async Task GetXkcdAsync(int number)
        {
            try
            {
                var comic = await xkcdService.GetComicAsync(number).ConfigureAwait(false);
                await EmbedComicAsync(comic, Context.Channel).ConfigureAwait(false);
            }
            catch (ArgumentOutOfRangeException)
            {
                await ReplyAsync("The specified comic does not exist!").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error while getting xkcd comic");
            }

        }

        private async Task EmbedComicAsync(XkcdResponse comic, IMessageChannel channel)
        {
            if (comic == null)
                return;
            var builder = new EmbedBuilder();
            string comicUrl = xkcdService.GetComicUrl(comic.Number).ToString();
            builder.WithImageUrl(comic.ImgUrl.ToString());
            builder.WithAuthor($"xkcd #{comic.Number}", "https://xkcd.com/s/919f27.ico", comicUrl);
            builder.WithTitle(comic.SafeTitle);
            builder.WithDescription(comic.Alt);
            if (int.TryParse(comic.Year, out int year) && int.TryParse(comic.Month, out int month) && int.TryParse(comic.Day, out int day))
            {
                DateTime date = new DateTime(year, month, day);
                builder.WithFooter(date.ToString("yyyy-MM-dd"));
            }
            await channel.SendMessageAsync("", false, builder.Build()).ConfigureAwait(false);
        }
    }
}