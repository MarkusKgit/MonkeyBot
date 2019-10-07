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
    [Name("Dog pictures")]
    public class DogModule : MonkeyModuleBase
    {
        private readonly IDogService dogService;

        public DogModule(IDogService dogService)
        {
            this.dogService = dogService;
        }

        [Command("Dog")]
        [Alias("Dogger", "Doggo")]
        [Remarks("Gets a random Dog picture.")]
        public async Task GetDogPicAsync()
        {
            string pictureURL = await (dogService?.GetDogPictureUrlAsync()).ConfigureAwait(false);
            if (pictureURL.IsEmpty())
            {
                _ = await ReplyAsync("Could not get a doggo pic :(").ConfigureAwait(false);
                return;
            }
            var builder = new EmbedBuilder().WithImageUrl(pictureURL);
            _ = await ReplyAsync(embed: builder.Build()).ConfigureAwait(false);
        }

        [Command("Dog")]
        [Alias("Dogger", "Doggo")]
        [Remarks("Gets a random Dog picture for the given breed.")]
        public async Task GetDogPicAsync([Remainder][Summary("The breed of the dogger")] string breed)
        {
            if (breed.IsEmpty())
            {
                _ = await ReplyAsync("Please provide a breed").ConfigureAwait(false);
                return;
            }

            string pictureURL = await (dogService?.GetDogPictureUrlAsync(breed.ToLowerInvariant())).ConfigureAwait(false);
            if (pictureURL.IsEmpty())
            {
                _ = await ReplyAsync("Could not get a doggo pic :(. Try using !dogbreeds to get a list of dog breeds I can show you").ConfigureAwait(false);
                return;
            }
            var builder = new EmbedBuilder().WithImageUrl(pictureURL);
            _ = await ReplyAsync(embed: builder.Build()).ConfigureAwait(false);
        }

        [Command("Dogbreeds")]
        [Alias("Doggerbreeds", "Doggobreeds")]
        [Remarks("Gets a list of available dog breeds.")]
        public async Task GetDogBreedsAsync()
        {
            List<string> breeds = await (dogService?.GetDogBreedsAsync()).ConfigureAwait(false);
            if (breeds == null || !breeds.Any())
            {
                _ = await ReplyAsync("Could not get a the dog breeds :(").ConfigureAwait(false);
                return;
            }
            _ = await ReplyAsync("Here's a list of available dog breeds:" + Environment.NewLine + string.Join(", ", breeds)).ConfigureAwait(false);
        }
    }
}