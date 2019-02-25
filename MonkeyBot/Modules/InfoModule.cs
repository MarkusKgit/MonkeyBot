using Discord;
using Discord.Commands;
using dokas.FluentStrings;
using MonkeyBot.Common;
using MonkeyBot.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MonkeyBot.Modules
{
    [Name("Info")]
    public class InfoModule : MonkeyModuleBase
    {
        private readonly DbService dbService;

        public InfoModule(DbService db)
        {
            dbService = db;
        }

        [Command("Rules")]
        [Remarks("The bot replies with the server rules in a private message")]
        [RequireContext(ContextType.Guild)]
        public async Task ListRulesAsync()
        {
            using (var uow = dbService.UnitOfWork)
            {
                var rules = (await uow.GuildConfigs.GetAsync(Context.Guild.Id))?.Rules;
                if (rules == null || rules.Count < 1)
                {
                    await ReplyAsync("No rules set!");
                    return;
                }
                var builder = new EmbedBuilder
                {
                    Color = new Color(255, 0, 0)
                };
                builder.AddField($"Rules of {Context.Guild.Name}:", string.Join(Environment.NewLine, rules));
                await Context.User.SendMessageAsync("", false, builder.Build());
                await ReplyAndDeleteAsync("I have sent you a private message");
            }
        }

        [Command("FindMessageID")]
        [Remarks("Gets the message id of a message in the current channel with the provided message text")]
        [RequireContext(ContextType.Guild)]
        public async Task FindMessageIDAsync([Summary("The content of the message to search for")][Remainder] string messageContent)
        {
            if (messageContent.IsEmpty().OrWhiteSpace())
            {
                await ReplyAsync("You need to specify the text of the message to search for");
                return;
            }
            const int searchDepth = 100;
            var messages = await Context.Channel.GetMessagesAsync(searchDepth).FlattenAsync();
            var matches = messages.Where(x => x.Content.ToLowerInvariant().StartsWith(messageContent.ToLowerInvariant().Trim()));
            if (matches == null || matches.Count() < 1)
            {
                await ReplyAsync($"Message not found. Hint: Only the last {searchDepth} messages in this channel are scanned.");
                return;
            }
            else if (matches.Count() > 1)
            {
                await ReplyAsync($"{matches.Count()} Messages found. Please be more specific");
                return;
            }
            else
            {
                await ReplyAsync($"The message Id is: {matches.First().Id}");
            }
        }
    }
}