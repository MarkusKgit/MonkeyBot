using Discord;
using Discord.Commands;
using MonkeyBot.Common;
using MonkeyBot.Preconditions;
using MonkeyBot.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MonkeyBot.Modules
{
    [MinPermissions(AccessLevel.User)]
    [RequireContext(ContextType.Guild)]
    [Name("Picture search")]
    public class PictureSearchModule : MonkeyModuleBase
    {
        private readonly IPictureSearchService pictureSearchService;

        public PictureSearchModule(IPictureSearchService pictureSearchService)
        {
            this.pictureSearchService = pictureSearchService;
        }

        [Command("Picture")]
        [Alias("Pic")]
        [Remarks("Gets a random picture for the given search term.")]
        public async Task GetPicAsync([Remainder][Summary("The term to search for")] string searchterm)
        {
            if (searchterm.IsEmpty())
            {
                _ = await ReplyAsync("Please provide a search term").ConfigureAwait(false);
                return;
            }

            string pictureURL = await (pictureSearchService?.GetRandomPictureUrlAsync(searchterm)).ConfigureAwait(false);
            if (pictureURL.IsEmpty())
            {
                _ = await ReplyAsync($"Could not get a picture for {searchterm} :(.").ConfigureAwait(false);
                return;
            }

            var builder = new EmbedBuilder()
                .WithImageUrl(pictureURL);            

            _ = await ReplyAsync(embed: builder.Build()).ConfigureAwait(false);
        }
    }
}
