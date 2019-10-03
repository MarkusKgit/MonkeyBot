using Discord;
using Discord.Commands;
using Microsoft.EntityFrameworkCore;
using MonkeyBot.Common;
using MonkeyBot.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MonkeyBot.Modules
{
    [Name("Info")]
    public class InfoModule : MonkeyModuleBase
    {
        private readonly MonkeyDBContext dbContext;

        public InfoModule(MonkeyDBContext dbContext)
        {
            this.dbContext = dbContext;
        }

        [Command("Rules")]
        [Remarks("The bot replies with the server rules in a private message")]
        [RequireContext(ContextType.Guild)]
        public async Task ListRulesAsync()
        {
            List<string> rules = (await dbContext.GuildConfigs.SingleOrDefaultAsync(x => x.GuildID == Context.Guild.Id).ConfigureAwait(false))?.Rules;
            if (rules == null || rules.Count < 1)
            {
                await ReplyAsync("No rules set!").ConfigureAwait(false);
                return;
            }
            var builder = new EmbedBuilder
            {
                Color = new Color(255, 0, 0)
            };
            builder.AddField($"Rules of {Context.Guild.Name}:", string.Join(Environment.NewLine, rules));
            await Context.User.SendMessageAsync("", false, builder.Build()).ConfigureAwait(false);
            await ReplyAndDeleteAsync("I have sent you a private message").ConfigureAwait(false);

        }

        [Command("FindMessageID")]
        [Remarks("Gets the message id of a message in the current channel with the provided message text")]
        [RequireContext(ContextType.Guild)]
        public async Task FindMessageIDAsync([Summary("The content of the message to search for")][Remainder] string messageContent)
        {
            if (messageContent.IsEmptyOrWhiteSpace())
            {
                await ReplyAsync("You need to specify the text of the message to search for").ConfigureAwait(false);
                return;
            }
            const int searchDepth = 100;
            IEnumerable<IMessage> messages = await Context.Channel.GetMessagesAsync(searchDepth).FlattenAsync().ConfigureAwait(false);
            IEnumerable<IMessage> matches = messages.Where(x => x.Content.StartsWith(messageContent.Trim(), StringComparison.OrdinalIgnoreCase));
            if (matches == null || !matches.Any())
            {
                await ReplyAsync($"Message not found. Hint: Only the last {searchDepth} messages in this channel are scanned.").ConfigureAwait(false);
                return;
            }
            else if (matches.Count() > 1)
            {
                await ReplyAsync($"{matches.Count()} Messages found. Please be more specific").ConfigureAwait(false);
                return;
            }
            else
            {
                await ReplyAsync($"The message Id is: {matches.First().Id}").ConfigureAwait(false);
            }
        }
    }
}