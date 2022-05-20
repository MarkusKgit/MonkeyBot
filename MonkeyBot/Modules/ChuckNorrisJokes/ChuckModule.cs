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
        private readonly IChuckService _chuckService;

        public ChuckModule(IChuckService chuckService)
        {
            _chuckService = chuckService;
        }

        [Command("Chuck")]        
        [Description("Gets a random Chuck Norris fact and replaces Chuck Norris with the given name.")]
        public async Task GetChuckFactAsync(CommandContext ctx, [RemainingText][Description("The person to chuck")] DiscordMember user = null)
        {
            await ctx.TriggerTypingAsync();            
            string fact = user == null 
                ? await (_chuckService?.GetChuckFactAsync()) 
                : await (_chuckService?.GetChuckFactAsync(user.DisplayName));            
            string title = user == null
                ? "Random Chuck Norris fact"
                : $"Random Chuck \"*{user.DisplayName}*\" Norris fact";
            _ = fact.IsEmpty()
               ? await ctx.ErrorAsync("Could not get a chuck fact :(")
               : await ctx.OkAsync(fact, title, false);

        }
    }
}