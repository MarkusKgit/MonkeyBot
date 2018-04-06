using Discord;
using Discord.Commands;
using MonkeyBot.Common;
using MonkeyBot.Preconditions;
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
    public class ModeratorModule : ModuleBase
    {
        [Command("Prune")]
        [Remarks("Adds the specified user to the list of bot owners")]
        [RequireUserPermission(ChannelPermission.ManageMessages)]
        [RequireBotPermission(ChannelPermission.ManageMessages)]
        public async Task PruneAsync(int count = 100)
        {
            if (count < 1)
            {
                await ReplyAsync("Count has to be at least 1");
                return;
            }
            if (count > 1000)
                count = 1000;
            var msgs = await Context.Channel.GetMessagesAsync(count).Flatten();
            await Context.Channel.DeleteMessagesAsync(msgs);
        }

        [Command("Prune")]
        [Remarks("Adds the specified user to the list of bot owners")]
        [RequireUserPermission(ChannelPermission.ManageMessages)]
        [RequireBotPermission(ChannelPermission.ManageMessages)]
        public async Task PruneAsync(IGuildUser user, int count = 100)
        {
            if (user == null)
            {
                await ReplyAsync("Invalid user");
                return;
            }
            if (count < 1)
            {
                await ReplyAsync("Count has to be at least 1");
                return;
            }
            if (count > 1000)
                count = 1000;
            var msgs = (await Context.Channel.GetMessagesAsync(count).Flatten()).Where(x => x.Author.Id == user.Id);
            await Context.Channel.DeleteMessagesAsync(msgs);
        }
    }
}