using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using MonkeyBot.Common;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MonkeyBot.Modules
{
    /// <summary>
    /// Provides moderator level commands
    /// </summary>
    [MinPermissions(AccessLevel.ServerMod)]
    [Description("Moderator Commands")]
    [RequireGuild]
    public class ModeratorModule : BaseCommandModule
    {
        [Command("Prune")]
        [Description("Deletes the specified amount of messages")]
        [MinPermissions(AccessLevel.ServerMod)]
        [Example("Prune 10")]
        public async Task PruneAsync(CommandContext ctx, [Description("The amount of messages to delete")] int count = 10)
        {
            if (count < 1)
            {
                _ = await ctx.ErrorAsync("Count has to be at least 1");
                return;
            }
            if (count > 100)
            {
                count = 100;
            }
            if (ctx.Channel.Type == ChannelType.Text)
            {
                IEnumerable<DiscordMessage> msgs = await ctx.Channel.GetMessagesAsync(count);
                await ctx.Channel.DeleteMessagesAsync(msgs);
            }
        }

        [Command("Prune")]
        [Description("Deletes the specified amount of messages for the specified user")]
        [MinPermissions(AccessLevel.ServerMod)]
        [Example("Prune @JohnDoe 10")]
        public async Task PruneAsync(CommandContext ctx, [Description("The user whose messages should be deleted")] DiscordUser user, [Description("The amount of the user's messages to delete")] int count = 10)
        {
            if (user == null)
            {
                await ctx.ErrorAsync("Invalid user");
                return;
            }
            if (count < 1)
            {
                _ = await ctx.ErrorAsync("Count has to be at least 1");
                return;
            }
            if (count > 100)
            {
                count = 100;
            }
            if (ctx.Channel.Type == ChannelType.Text)
            {
                IEnumerable<DiscordMessage> msgs = (await ctx.Channel.GetMessagesAsync(100)).Where(msg => msg.Author.Id == user.Id).Take(count);
                await ctx.Channel.DeleteMessagesAsync(msgs);
            }
        }
    }
}