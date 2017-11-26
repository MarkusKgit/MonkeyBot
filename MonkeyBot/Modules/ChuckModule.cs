using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
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
    public class ChuckModule : ModuleBase
    {
        private IChuckService chuckService;

        public ChuckModule(IServiceProvider provider)
        {
            chuckService = provider.GetService<IChuckService>();
        }

        [Command("Chuck")]
        [Remarks("Gets a random Chuck Norris fact.")]
        public async Task GetChuckFactAsync()
        {
            var fact = await chuckService?.GetChuckFactAsync();
            if (string.IsNullOrEmpty(fact))
            {
                await ReplyAsync("Could not get a chuck fact :(");
                return;
            }
            await ReplyAsync(fact);
        }

        [Command("Chuck")]
        [Remarks("Gets a random Chuck Norris fact and replaces Chuck Norris with the given name.")]
        public async Task GetChuckFactAsync([Remainder][Summary("The name of the person to chuck")] string name)
        {
            var user = (await Context.Guild?.GetUsersAsync())?.Where(x => x.Username.ToLower().Contains(name.ToLower()));
            if (user != null || user.Count() == 1)
            {
                var fact = await chuckService?.GetChuckFactAsync(name);
                if (string.IsNullOrEmpty(fact))
                {
                    await ReplyAsync("Could not get a chuck fact :(");
                    return;
                }
                fact = fact.Replace(name, user.First().Mention);
                await ReplyAsync(fact);
            }
            else if (user == null)
                await ReplyAsync("User not found");
            else
                await ReplyAsync("Multiple users found! Please be more specific");
        }
    }
}