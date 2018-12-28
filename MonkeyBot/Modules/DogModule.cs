using Discord;
using Discord.Commands;
using dokas.FluentStrings;
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
    [Name("Chuck Norris jokes")]
    public class DogModule : ModuleBase
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
            string pictureURL = await dogService?.GetDogPictureUrlAsync();
            if (pictureURL.IsEmpty())
            {
                await ReplyAsync("Could not get a doggo pic :(");
                return;
            }
            var builder = new EmbedBuilder().WithImageUrl(pictureURL);
            await ReplyAsync(embed: builder.Build());
        }

        [Command("Dog")]
        [Alias("Dogger", "Doggo")]
        [Remarks("Gets a random Dog picture for the given breed.")]
        public async Task GetDogPicAsync([Remainder][Summary("The breed of the dogger")] string breed)
        {
            if (breed.IsEmpty())
            {
                await ReplyAsync("Please provide a breed");
                return;
            }

            string pictureURL = await dogService?.GetDogPictureUrlAsync(breed.ToLowerInvariant());
            if (pictureURL.IsEmpty())
            {
                await ReplyAsync("Could not get a doggo pic :(. Try using !dogbreeds to get a list of dog breeds I can show you");
                return;
            }
            var builder = new EmbedBuilder().WithImageUrl(pictureURL);
            await ReplyAsync(embed: builder.Build());
        }

        [Command("Dogbreeds")]
        [Alias("Doggerbreeds", "Doggobreeds")]
        [Remarks("Gets a list of available dog breeds.")]
        public async Task GetDogBreedsAsync()
        {
            List<string> breeds = await dogService?.GetDogBreedsAsync();
            if (breeds == null || !breeds.Any())
            {
                await ReplyAsync("Could not get a the dog breeds :(");
                return;
            }
            await ReplyAsync("Here's a list of available dog breeds:" + Environment.NewLine + string.Join(", ", breeds));
        }
    }
}