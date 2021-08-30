using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using MonkeyBot.Common;
using MonkeyBot.Services;
using System;
using System.Threading.Tasks;

namespace MonkeyBot.Modules
{
    [MinPermissions(AccessLevel.User)]
    [RequireGuild]
    [Description("Picture search")]
    public class PictureSearchModule : BaseCommandModule
    {
        private readonly IPictureSearchService _pictureSearchService;

        public PictureSearchModule(IPictureSearchService pictureSearchService)
        {
            _pictureSearchService = pictureSearchService;
        }

        [Command("Picture")]
        [Aliases("Pic")]
        [Description("Gets a random picture for the given search term.")]
        public async Task GetPicAsync(CommandContext ctx, [RemainingText, Description("The term to search for")] string searchterm)
        {
            await ctx.TriggerTypingAsync();

            if (searchterm.IsEmpty())
            {
                _ = await ctx.ErrorAsync("Please provide a search term");
                return;
            }

            Uri pictureURL = await (_pictureSearchService?.GetRandomPictureUrlAsync(searchterm));
            if (pictureURL == null)
            {
                _ = await ctx.ErrorAsync($"Could not get a picture for {searchterm} :(.");
                return;
            }

            var builder = new DiscordEmbedBuilder()
                .WithTitle($"Random image for \"{searchterm}\"")
                .WithImageUrl(pictureURL);

            _ = await ctx.RespondAsync(builder.Build());
        }
    }
}
