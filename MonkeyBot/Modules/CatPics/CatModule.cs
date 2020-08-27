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
    public class CatModule : BaseCommandModule
    {
        private static readonly DiscordColor catColor = DiscordColor.Goldenrod;
        
        private readonly ICatService catService;

        public CatModule(ICatService catService)
        {
            this.catService = catService;
        }

        [Command("Cat")]
        [Aliases("Cate", "Kitty", "Pussy")]
        [Description("Gets a random Cat picture. Optionally a breed can be provided.")]
        public async Task GetCatPicAsync(CommandContext ctx, [RemainingText, Description("Optional: The breed of the cat")] string breed = "")
        {
            await ctx.TriggerTypingAsync().ConfigureAwait(false);
            Uri pictureURL = breed.IsEmpty() ?
                await (catService?.GetRandomPictureUrlAsync()).ConfigureAwait(false):
                await (catService?.GetRandomPictureUrlAsync(breed.ToLowerInvariant())).ConfigureAwait(false);
            if (pictureURL == null)
            {
                _ = await ctx.ErrorAsync($"Could not get a cat pic :(. Try using {ctx.Prefix}catbreeds to get a list of cat breeds I can show you").ConfigureAwait(false);
                return;
            }
            DiscordEmbedBuilder builder = new DiscordEmbedBuilder()
                .WithColor(catColor)
                .WithImageUrl(pictureURL);
            _ = await ctx.RespondDeletableAsync(embed: builder.Build()).ConfigureAwait(false);
        }

        [Command("Catbreeds")]
        [Aliases("Catebreeds", "Kittybreeds", "Pussybreeds")]
        [Description("Gets a list of available cat breeds.")]
        public async Task GetCatBreedsAsync(CommandContext ctx)
        {
            await ctx.TriggerTypingAsync().ConfigureAwait(false);
            List<string> breeds = await (catService?.GetBreedsAsync()).ConfigureAwait(false);
            if (breeds == null || !breeds.Any())
            {
                _ = await ctx.ErrorAsync("Could not get the cat breeds :(").ConfigureAwait(false);
                return;
            }
            DiscordEmbedBuilder builder = new DiscordEmbedBuilder()
                .WithColor(catColor)
                .WithTitle("Here's a list of available cat breeds:")
                .WithDescription(string.Join(", ", breeds));
            _ = await ctx.RespondDeletableAsync(embed: builder.Build()).ConfigureAwait(false);
        }
    }
}
