using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using MonkeyBot.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MonkeyBot.Modules
{
    /// <summary>
    /// Provides some commands for the bot owner
    /// </summary>
    [MinPermissions(AccessLevel.BotOwner)]
    [Description("Owner Commands")]
    public class OwnerModule : BaseCommandModule
    {
        [Command("Say")]
        [Description("Say something in a specific guild's channel")]
        public async Task SayAsync(CommandContext ctx, ulong guildId, ulong channelId, [RemainingText] string message)
        {
            DiscordGuild guild = await ctx.Client.GetGuildAsync(guildId).ConfigureAwait(false);
            if (guild == null)
            {
                _ = await ctx.ErrorAsync("Guild not found").ConfigureAwait(false);
                return;
            }
            DiscordChannel channel = guild.GetChannel(channelId);
            if (channel == null)
            {
                _ = await ctx.ErrorAsync("Channel not found").ConfigureAwait(false);
                return;
            }
            _ = await channel.SendMessageAsync(message).ConfigureAwait(false);
        }

        [Command("ListGuilds")]
        [Description("List all the guilds the Bot joined")]
        public async Task ListGuildsAsync(CommandContext ctx)
        {
            IEnumerable<DiscordGuild> guilds = ctx.Client.Guilds.Values;
            var builder = new DiscordEmbedBuilder()
                .WithAuthor(ctx.Client.CurrentUser.Username)
                .WithTitle("Currently connected guilds");
            foreach (DiscordGuild guild in guilds)
            {
                int channelCount = (await guild.GetChannelsAsync().ConfigureAwait(false)).Count;
                int userCount = (await guild.GetAllMembersAsync().ConfigureAwait(false)).Count;
                string guildInfo = $"{channelCount} Channels, {userCount} Users, Owned by {guild.Owner.Username}, Created at {guild.CreationTimestamp}";
                if (!guild.Description.IsEmptyOrWhiteSpace())
                {
                    guildInfo += $"\n Description: {guild.Description}";
                }
                _ = builder.AddField(guild.Name, guildInfo);
            }
            _ = await ctx.RespondAsync(embed: builder.Build()).ConfigureAwait(false);
        }

        [Command("AddOwner")]
        [Description("Adds the specified user to the list of bot owners")]
        public async Task AddOwnerAsync(CommandContext ctx, [Description("The user to add")] DiscordUser user)
        {
            if (user == null)
            {
                await ctx.ErrorAsync("Invalid user").ConfigureAwait(false);
                return;
            }
            DiscordClientConfiguration config = await DiscordClientConfiguration.LoadAsync().ConfigureAwait(false);
            if (!config.Owners.Contains(user.Id))
            {
                config.AddOwner(user.Id);
                await config.SaveAsync().ConfigureAwait(false);
                _ = await ctx.OkAsync($"{user.Username} has been added to the list of bot owners!").ConfigureAwait(false);
            }
            else
            {
                _ = await ctx.ErrorAsync($"{user.Username} already is a bot owner!").ConfigureAwait(false);
            }
        }

        [Command("RemoveOwner")]
        [Description("Removes the specified user from the list of bot owners")]
        [MinPermissions(AccessLevel.BotOwner)]
        public async Task RemoveOwnerAsync(CommandContext ctx, [Description("The user to remove")] DiscordUser user)
        {
            if (user == null)
            {
                await ctx.ErrorAsync("Invalid user").ConfigureAwait(false);
                return;
            }
            DiscordClientConfiguration config = await DiscordClientConfiguration.LoadAsync().ConfigureAwait(false);
            if (config.Owners.Contains(user.Id))
            {
                config.RemoveOwner(user.Id);
                await config.SaveAsync().ConfigureAwait(false);
                _ = await ctx.OkAsync($"{user.Username} has been removed from the list of bot owners!").ConfigureAwait(false);
            }
            else
            {
                _ = await ctx.ErrorAsync($"{user.Username} is not a bot owner!").ConfigureAwait(false);
            }
        }
    }
}