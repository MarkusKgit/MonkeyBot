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
    [RequireGuild]
    [Description("Dog pictures")]
    public class DogModule : MonkeyModuleBase
    {
        private readonly IDogService dogService;

        public DogModule(IDogService dogService)
        {
            this.dogService = dogService;
        }

        [Command("Dog")]
        [Aliases("Dogger", "Doggo")]
        [Description("Gets a random Dog picture.")]
        public async Task GetDogPicAsync()
        {
            string pictureURL = await (dogService?.GetDogPictureUrlAsync()).ConfigureAwait(false);
            if (pictureURL.IsEmpty())
            {
                _ = await ctx.RespondAsync("Could not get a doggo pic :(").ConfigureAwait(false);
                return;
            }
            var builder = new DiscordEmbedBuilder().WithImageUrl(pictureURL);
            _ = await ctx.RespondAsync(embed: builder.Build()).ConfigureAwait(false);
        }

        [Command("Dog")]
        [Aliases("Dogger", "Doggo")]
        [Description("Gets a random Dog picture for the given breed.")]
        public async Task GetDogPicAsync([RemainingText][Summary("The breed of the dogger")] string breed)
        {
            if (breed.IsEmpty())
            {
                _ = await ctx.RespondAsync("Please provide a breed").ConfigureAwait(false);
                return;
            }

            string pictureURL = await (dogService?.GetDogPictureUrlAsync(breed.ToLowerInvariant())).ConfigureAwait(false);
            if (pictureURL.IsEmpty())
            {
                _ = await ctx.RespondAsync("Could not get a doggo pic :(. Try using !dogbreeds to get a list of dog breeds I can show you").ConfigureAwait(false);
                return;
            }
            var builder = new DiscordEmbedBuilder().WithImageUrl(pictureURL);
            _ = await ctx.RespondAsync(embed: builder.Build()).ConfigureAwait(false);
        }

        [Command("Dogbreeds")]
        [Aliases("Doggerbreeds", "Doggobreeds")]
        [Description("Gets a list of available dog breeds.")]
        public async Task GetDogBreedsAsync()
        {
            List<string> breeds = await (dogService?.GetDogBreedsAsync()).ConfigureAwait(false);
            if (breeds == null || !breeds.Any())
            {
                _ = await ctx.RespondAsync("Could not get a the dog breeds :(").ConfigureAwait(false);
                return;
            }
            _ = await ctx.RespondAsync("Here's a list of available dog breeds:" + Environment.NewLine + string.Join(", ", breeds)).ConfigureAwait(false);
        }
    }
}