using Discord;
using Discord.Commands;
using MonkeyBot.Common;
using MonkeyBot.Preconditions;
using MonkeyBot.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MonkeyBot.Modules
{
    [MinPermissions(AccessLevel.User)]
    [RequireContext(ContextType.Guild)]
    [Name("Cat pictures")]
    public class CatModule : MonkeyModuleBase
    {
        private readonly ICatService catService;

        public CatModule(ICatService catService)
        {
            this.catService = catService;
        }

        [Command("Cat")]
        [Alias("Cate", "Kitty", "Pussy")]
        [Remarks("Gets a random cat picture.")]
        public async Task GetCatPicAsync()
        {
            string pictureURL = await (catService?.GetCatPictureUrlAsync()).ConfigureAwait(false);
            if (pictureURL.IsEmpty())
            {
                _ = await ReplyAsync("Could not get a cat pic :(").ConfigureAwait(false);
                return;
            }
            var builder = new EmbedBuilder().WithImageUrl(pictureURL);
            _ = await ReplyAsync(embed: builder.Build()).ConfigureAwait(false);
        }

        [Command("Cat")]
        [Alias("Cate", "Kitty", "Pussy")]
        [Remarks("Gets a random Cat picture for the given breed.")]
        public async Task GetCatPicAsync([Remainder][Summary("The breed of the cat")] string breed)
        {
            if (breed.IsEmpty())
            {
                _ = await ReplyAsync("Please provide a breed").ConfigureAwait(false);
                return;
            }

            string pictureURL = await (catService?.GetCatPictureUrlAsync(breed.ToLowerInvariant())).ConfigureAwait(false);
            if (pictureURL.IsEmpty())
            {
                _ = await ReplyAsync("Could not get a cat pic :(. Try using !catbreeds to get a list of cat breeds I can show you").ConfigureAwait(false);
                return;
            }
            var builder = new EmbedBuilder().WithImageUrl(pictureURL);
            _ = await ReplyAsync(embed: builder.Build()).ConfigureAwait(false);
        }

        [Command("Catbreeds")]
        [Alias("Catebreeds", "Kittybreeds", "Pussybreeds")]
        [Remarks("Gets a list of available cat breeds.")]
        public async Task GetCatBreedsAsync()
        {
            List<string> breeds = await (catService?.GetCatBreedsAsync()).ConfigureAwait(false);
            if (breeds == null || !breeds.Any())
            {
                _ = await ReplyAsync("Could not get the cat breeds :(").ConfigureAwait(false);
                return;
            }
            _ = await ReplyAsync("Here's a list of available cat breeds:" + Environment.NewLine + string.Join(", ", breeds)).ConfigureAwait(false);
        }
    }
}
