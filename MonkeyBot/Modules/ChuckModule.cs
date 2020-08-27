using Discord;
using Discord.Commands;
using MonkeyBot.Common;
using MonkeyBot.Preconditions;
using MonkeyBot.Services;
using System;
using System.Threading.Tasks;

namespace MonkeyBot.Modules
{
    [MinPermissions(AccessLevel.User)]
    [RequireGuild]
    [Description("Chuck Norris jokes")]
    public class ChuckModule : MonkeyModuleBase
    {
        private readonly IChuckService chuckService;

        public ChuckModule(IChuckService chuckService)
        {
            this.chuckService = chuckService;
        }

        [Command("Chuck")]
        [Description("Gets a random Chuck Norris fact.")]
        public async Task GetChuckFactAsync()
        {
            string fact = await (chuckService?.GetChuckFactAsync()).ConfigureAwait(false);
            _ = fact.IsEmpty()
                ? await ctx.RespondAsync("Could not get a chuck fact :(").ConfigureAwait(false)
                : await ctx.RespondAsync(fact).ConfigureAwait(false);
        }

        [Command("Chuck")]
        [Description("Gets a random Chuck Norris fact and replaces Chuck Norris with the given name.")]
        public async Task GetChuckFactAsync([RemainingText][Summary("The name of the person to chuck")] string username)
        {
            IGuildUser user = await GetUserInGuildAsync(username).ConfigureAwait(false);
            if (user == null)
            {
                return;
            }
            string fact = await (chuckService?.GetChuckFactAsync(username)).ConfigureAwait(false);
            if (fact.IsEmpty())
            {
                _ = await ctx.RespondAsync("Could not get a chuck fact :(").ConfigureAwait(false);
                return;
            }
            fact = fact.Replace(username, user.Mention);
            _ = await ctx.RespondAsync(fact).ConfigureAwait(false);
        }
    }
}