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
        public async Task SayAsync(CommandContext ctx,
                                   [Description("Id of the guild where to post")] ulong guildId,
                                   [Description("Id of the text channel where to post")] ulong channelId,
                                   [RemainingText, Description("Message to post")] string message)
        {
            DiscordGuild guild = await ctx.Client.GetGuildAsync(guildId);
            if (guild == null)
            {
                await ctx.ErrorAsync("Guild not found");
                return;
            }
            DiscordChannel channel = guild.GetChannel(channelId);
            if (channel == null)
            {
                await ctx.ErrorAsync("Channel not found");
                return;
            }
            await channel.SendMessageAsync(message);
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
                int channelCount = (await guild.GetChannelsAsync()).Count;
                int userCount = (await guild.GetAllMembersAsync()).Count;
                string guildInfo = $"{channelCount} Channels, {userCount} Users, Owned by {guild.Owner.Username}, Created at {guild.CreationTimestamp}";
                if (!guild.Description.IsEmptyOrWhiteSpace())
                {
                    guildInfo += $"\n Description: {guild.Description}";
                }
                builder.AddField(guild.Name, guildInfo);
            }
            await ctx.RespondAsync(embed: builder.Build());
        }

        [Command("AddOwner")]
        [Description("Adds the specified user to the list of bot owners")]
        public async Task AddOwnerAsync(CommandContext ctx, [Description("The user to add as an owner")] DiscordUser user)
        {
            if (user == null)
            {
                await ctx.ErrorAsync("Invalid user");
                return;
            }
            DiscordClientConfiguration config = await DiscordClientConfiguration.LoadAsync();
            if (!config.Owners.Contains(user.Id))
            {
                config.AddOwner(user.Id);
                await config.SaveAsync();
                await ctx.OkAsync($"{user.Username} has been added to the list of bot owners!");
            }
            else
            {
                await ctx.ErrorAsync($"{user.Username} already is a bot owner!");
            }
        }

        [Command("RemoveOwner")]
        [Description("Removes the specified user from the list of bot owners")]
        [MinPermissions(AccessLevel.BotOwner)]
        public async Task RemoveOwnerAsync(CommandContext ctx, [Description("The user to remove from the owners")] DiscordUser user)
        {
            if (user == null)
            {
                await ctx.ErrorAsync("Invalid user");
                return;
            }
            DiscordClientConfiguration config = await DiscordClientConfiguration.LoadAsync();
            if (config.Owners.Contains(user.Id))
            {
                config.RemoveOwner(user.Id);
                await config.SaveAsync();
                await ctx.OkAsync($"{user.Username} has been removed from the list of bot owners!");
            }
            else
            {
                await ctx.ErrorAsync($"{user.Username} is not a bot owner!");
            }
        }
    }
}