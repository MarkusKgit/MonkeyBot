using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using MonkeyBot.Common;
using MonkeyBot.Services;
using System;
using System.Threading.Tasks;

namespace MonkeyBot.Modules
{
    [MinPermissions(AccessLevel.User)]
    [RequireGuild]
    [Description("Chuck Norris jokes")]
    public class ChuckModule : BaseCommandModule
    {
        private readonly IChuckService chuckService;

        public ChuckModule(IChuckService chuckService)
        {
            this.chuckService = chuckService;
        }

        [Command("Chuck")]
        [Description("Gets a random Chuck Norris fact.")]
        public async Task GetChuckFactAsync(CommandContext ctx)
        {
            await ctx.TriggerTypingAsync().ConfigureAwait(false);
            string fact = await (chuckService?.GetChuckFactAsync()).ConfigureAwait(false);
            _ = fact.IsEmpty()
                ? await ctx.ErrorAsync("Could not get a chuck fact :(").ConfigureAwait(false)
                : await ctx.OkAsync(fact, "Random Chuck Norris fact").ConfigureAwait(false);
        }

        [Command("Chuck")]
        [Description("Gets a random Chuck Norris fact and replaces Chuck Norris with the given name.")]
        public async Task GetChuckFactAsync(CommandContext ctx, [RemainingText][Description("The person to chuck")] DiscordUser user)
        {
            await ctx.TriggerTypingAsync().ConfigureAwait(false);
            if (user == null)
            {
                _ = await ctx.ErrorAsync("Invalid User").ConfigureAwait(false);
            }
            string fact = await (chuckService?.GetChuckFactAsync(user.Username)).ConfigureAwait(false);
            if (fact.IsEmpty())
            {
                _ = await ctx.ErrorAsync("Could not get a chuck fact :(").ConfigureAwait(false);
                return;
            }
            fact = fact.Replace(user.Username, user.Mention);
            _ = await ctx.OkAsync(fact, $"Random {user.Username} Norris fact").ConfigureAwait(false);
        }
    }
}