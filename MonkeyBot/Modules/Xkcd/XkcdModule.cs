using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;
using MonkeyBot.Common;
using MonkeyBot.Services;
using System;
using System.Threading.Tasks;

namespace MonkeyBot.Modules
{
    [Description("Xkcd comics")]
    public class XkcdModule : BaseCommandModule
    {
        private readonly IXkcdService _xkcdService;
        private readonly ILogger _logger;

        public XkcdModule(IXkcdService xkcdService, ILogger<XkcdModule> logger)
        {
            _xkcdService = xkcdService;
            _logger = logger;
        }

        [Command("xkcd")]
        [Description("Gets a random xkcd comic if the argument is left empty. Gets the latest xkcd comment by supplying \"latest\" as the arg or a specific comic by supplying the number")]
        [Example("xkcd")]
        [Example("xkcd 101")]
        [Example("xkcd latest")]
        public async Task GetXkcdAsync(CommandContext ctx, [Description("Random comic if left empty, specific comic by number or latest by supplying \"latest\"")] string arg = "")
        {
            XkcdResponse comic = null;

            if (arg.Equals("latest", StringComparison.OrdinalIgnoreCase))
            {
                comic = await _xkcdService.GetLatestComicAsync();
            }
            else if (int.TryParse(arg, out int comicNumber))
            {
                try
                {
                    comic = await _xkcdService.GetComicAsync(comicNumber);
                }
                catch (ArgumentOutOfRangeException)
                {
                    await ctx.ErrorAsync("The specified comic does not exist!");
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while getting xkcd comic");
                    return;
                }
            }
            else
            {
                comic = await _xkcdService.GetRandomComicAsync();
            }

            string comicUrl = _xkcdService.GetComicUrl(comic.Number).ToString();
            var builder = new DiscordEmbedBuilder()
                .WithImageUrl(comic.ImgUrl)
                .WithAuthor($"xkcd #{comic.Number}", comicUrl, "https://xkcd.com/s/0b7742.png")                
                .WithTitle(comic.SafeTitle)
                .WithDescription(comic.Alt);
            if (int.TryParse(comic.Year, out int year) && int.TryParse(comic.Month, out int month) && int.TryParse(comic.Day, out int day))
            {
                var date = new DateTime(year, month, day);
                builder.WithFooter(date.ToString("yyyy-MM-dd"));
            }
            await ctx.RespondAsync(builder.Build());
        }
    }
}