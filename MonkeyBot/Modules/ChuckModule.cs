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
    public class ChuckModule : ModuleBase
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
        public async Task GetChuckFactAsync([Remainder][Summary("The name of the person to chuck")] string name)
        {
            if (name.IsEmpty())
            {
                await ReplyAsync("Please provide a name");
                return;
            }

            IGuildUser user = null;
            if (name.StartsWith("<@") && ulong.TryParse(name.Replace("<@", "").Replace(">", ""), out var id))
                user = await Context.Guild.GetUserAsync(id);
            else
            {
                var users = (await Context.Guild?.GetUsersAsync())?.Where(x => x.Username.ToLower().Contains(name.ToLower()));
                if (users != null && users.Count() == 1)
                    user = users.First();
                else if (users == null)
                    await ReplyAsync("User not found");
                else
                    await ReplyAsync("Multiple users found! Please be more specific. Did you mean one of the following:"
                        + Environment.NewLine
                        + string.Join(", ", users.Select(x => x.Username))
                        );
            }
            if (user == null)
                return;
            var fact = await chuckService?.GetChuckFactAsync(name);
            if (fact.IsEmpty())
            {
                await ReplyAsync("Could not get a chuck fact :(");
                return;
            }
            fact = fact.Replace(name, user.Mention);
            await ReplyAsync(fact);
        }
    }
}