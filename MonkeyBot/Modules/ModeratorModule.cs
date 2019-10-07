using Discord;
using Discord.Commands;
using MonkeyBot.Common;
using MonkeyBot.Preconditions;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MonkeyBot.Modules
{
    /// <summary>
    /// Provides moderator level commands
    /// </summary>
    [MinPermissions(AccessLevel.ServerMod)]
    [Name("Moderator Commands")]
    [RequireContext(ContextType.Guild)]
    public class ModeratorModule : MonkeyModuleBase
    {
        [Command("Prune")]
        [Remarks("Deletes the specified amount of messages")]
        [RequireUserPermission(ChannelPermission.ManageMessages)]
        [RequireBotPermission(ChannelPermission.ManageMessages)]
        [Example("!Prune 10")]
        public async Task PruneAsync(int count = 10)
        {
            if (count < 1)
            {
                _ = await ReplyAsync("Count has to be at least 1").ConfigureAwait(false);
                return;
            }
            if (count > 100)
            {
                count = 100;
            }
            if (Context.Channel is ITextChannel channel)
            {
                IEnumerable<IMessage> msgs = await channel.GetMessagesAsync(count).FlattenAsync().ConfigureAwait(false);
                await channel.DeleteMessagesAsync(msgs).ConfigureAwait(false);
            }
        }

        [Command("Prune")]
        [Remarks("Deletes the specified amount of messages for the specified user")]
        [RequireUserPermission(ChannelPermission.ManageMessages)]
        [RequireBotPermission(ChannelPermission.ManageMessages)]
        [Example("!Prune JohnDoe 10")]
        public async Task PruneAsync(string userName, int count = 10)
        {
            IGuildUser user = await GetUserInGuildAsync(userName).ConfigureAwait(false);
            if (user == null)
            {
                return;
            }
            if (count < 1)
            {
                _ = await ReplyAsync("Count has to be at least 1").ConfigureAwait(false);
                return;
            }
            if (count > 100)
            {
                count = 100;
            }
            if (Context.Channel is ITextChannel channel)
            {
                IEnumerable<IMessage> msgs = (await channel.GetMessagesAsync(count).FlattenAsync().ConfigureAwait(false)).Where(x => x.Author.Id == user.Id);
                await channel.DeleteMessagesAsync(msgs).ConfigureAwait(false);
            }
        }
    }
}