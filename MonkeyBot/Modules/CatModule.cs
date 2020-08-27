using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using MonkeyBot.Common;
using MonkeyBot.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MonkeyBot.Modules
{
    [MinPermissions(AccessLevel.User)]
    [RequireGuild]    
    [Description("Cat pictures")]
    public class CatModule : MonkeyModuleBase
    {
        private readonly ICatService catService;

        public CatModule(ICatService catService)
        {
            this.catService = catService;
        }

        [Command("Cat")]
        [Aliases("Cate", "Kitty", "Pussy")]
        [Description("Gets a random cat picture.")]
        public async Task GetCatPicAsync(CommandContext ctx)
        {
            string pictureURL = await (catService?.GetCatPictureUrlAsync()).ConfigureAwait(false);
            if (pictureURL.IsEmpty())
            {
                _ = await ctx.RespondAsync("Could not get a cat pic :(").ConfigureAwait(false);
                return;
            }
            var builder = new DiscordEmbedBuilder().WithImageUrl(new Uri(pictureURL));
            _ = await ctx.RespondAsync(embed: builder.Build()).ConfigureAwait(false);
        }

        [Command("Cat")]
        [Aliases("Cate", "Kitty", "Pussy")]
        [Description("Gets a random Cat picture for the given breed.")]
        public async Task GetCatPicAsync(CommandContext ctx, [RemainingText, Description("The breed of the cat")] string breed)
        {
            if (breed.IsEmpty())
            {
                _ = await ctx.RespondAsync("Please provide a breed").ConfigureAwait(false);
                return;
            }

            string pictureURL = await (catService?.GetCatPictureUrlAsync(breed.ToLowerInvariant())).ConfigureAwait(false);
            if (pictureURL.IsEmpty())
            {
                _ = await ctx.RespondAsync("Could not get a cat pic :(. Try using !catbreeds to get a list of cat breeds I can show you").ConfigureAwait(false);
                return;
            }
            var builder = new DiscordEmbedBuilder().WithImageUrl(new Uri(pictureURL));
            _ = await ctx.RespondAsync(embed: builder.Build()).ConfigureAwait(false);
        }

        [Command("Catbreeds")]
        [Aliases("Catebreeds", "Kittybreeds", "Pussybreeds")]
        [Description("Gets a list of available cat breeds.")]
        public async Task GetCatBreedsAsync(CommandContext ctx)
        {
            List<string> breeds = await (catService?.GetCatBreedsAsync()).ConfigureAwait(false);
            if (breeds == null || !breeds.Any())
            {
                _ = await ctx.RespondAsync("Could not get the cat breeds :(").ConfigureAwait(false);
                return;
            }
            _ = await ctx.RespondAsync("Here's a list of available cat breeds:" + Environment.NewLine + string.Join(", ", breeds)).ConfigureAwait(false);
        }
    }
}
