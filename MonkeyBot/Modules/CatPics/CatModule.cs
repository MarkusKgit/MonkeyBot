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
            await ctx.TriggerTypingAsync();
            Uri pictureURL = breed.IsEmpty() ?
                await (catService?.GetRandomPictureUrlAsync()):
                await (catService?.GetRandomPictureUrlAsync(breed.ToLowerInvariant()));
            if (pictureURL == null)
            {
                _ = await ctx.ErrorAsync($"Could not get a cat pic :(. Try using {ctx.Prefix}catbreeds to get a list of cat breeds I can show you");
                return;
            }
            DiscordEmbedBuilder builder = new DiscordEmbedBuilder()
                .WithColor(catColor)
                .WithImageUrl(pictureURL);
            _ = await ctx.RespondAsync(embed: builder.Build());
        }

        [Command("Catbreeds")]
        [Aliases("Catebreeds", "Kittybreeds", "Pussybreeds")]
        [Description("Gets a list of available cat breeds.")]
        public async Task GetCatBreedsAsync(CommandContext ctx)
        {
            await ctx.TriggerTypingAsync();
            List<string> breeds = await (catService?.GetBreedsAsync());
            if (breeds == null || !breeds.Any())
            {
                _ = await ctx.ErrorAsync("Could not get the cat breeds :(");
                return;
            }
            DiscordEmbedBuilder builder = new DiscordEmbedBuilder()
                .WithColor(catColor)
                .WithTitle("Here's a list of available cat breeds:")
                .WithDescription(string.Join(", ", breeds));
            _ = await ctx.RespondDeletableAsync(builder.Build());
        }
    }
}
