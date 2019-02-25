using Discord;
using Discord.Commands;
using dokas.FluentStrings;
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
    [Name("Chuck Norris jokes")]
    public class ChuckModule : MonkeyModuleBase
    {
        private readonly IChuckService chuckService;

        public ChuckModule(IChuckService chuckService)
        {
            this.chuckService = chuckService;
        }

        [Command("Chuck")]
        [Remarks("Gets a random Chuck Norris fact.")]
        public async Task GetChuckFactAsync()
        {
            var fact = await chuckService?.GetChuckFactAsync();
            if (fact.IsEmpty())
            {
                await ReplyAsync("Could not get a chuck fact :(");
                return;
            }
            await ReplyAsync(fact);
        }

        [Command("Chuck")]
        [Remarks("Gets a random Chuck Norris fact and replaces Chuck Norris with the given name.")]
        public async Task GetChuckFactAsync([Remainder][Summary("The name of the person to chuck")] string username)
        {
            IGuildUser user = await GetUserInGuildAsync(username);
            if (user == null)
                return;
            var fact = await chuckService?.GetChuckFactAsync(username);
            if (fact.IsEmpty())
            {
                await ReplyAsync("Could not get a chuck fact :(");
                return;
            }
            fact = fact.Replace(username, user.Mention);
            await ReplyAsync(fact);
        }
    }
}