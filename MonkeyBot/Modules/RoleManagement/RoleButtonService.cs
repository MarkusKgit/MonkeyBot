using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MonkeyBot.Database;
using MonkeyBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyBot.Services
{
    public class RoleButtonService : IRoleButtonService
    {
        private readonly DiscordClient discordClient;

        private readonly MonkeyDBContext dbContext;

        private readonly ILogger<RoleButtonService> logger;

        public RoleButtonService(DiscordClient discordClient, MonkeyDBContext dbContext, ILogger<RoleButtonService> logger)
        {
            this.discordClient = discordClient;
            this.dbContext = dbContext;
            this.logger = logger;
        }

        public void Initialize()
        {
            discordClient.MessageReactionAdded += DiscordClient_MessageReactionAdded;
            discordClient.MessageReactionRemoved += DiscordClient_MessageReactionRemoved;
        }

        public async Task AddRoleButtonLinkAsync(ulong guildId, ulong channelId, ulong messageId, ulong roleId, string emojiString)
        {
            if (!discordClient.Guilds.TryGetValue(guildId, out DiscordGuild guild))
            {
                throw new ArgumentException("Invalid guild");
            }

            DiscordChannel channel = guild.GetChannel(channelId);
            if (channel == null)
            {
                throw new ArgumentException("Invalid channel");
            }

            DiscordMessage message = await channel.GetMessageAsync(messageId).ConfigureAwait(false);
            if (message == null)
            {
                throw new ArgumentException("Invalid message");
            }

            DiscordEmoji emoji = guild.Emojis.Values.FirstOrDefault(x => emojiString.Contains(x.Name, StringComparison.Ordinal)) ?? DiscordEmoji.FromName(discordClient, emojiString);
            if (emoji == null)
            {
                throw new ArgumentException("invalid emoji");
            }

            if (!message.Reactions.Any(r => r.Emoji == emoji))
            {
                await message.CreateReactionAsync(emoji).ConfigureAwait(false);
            }

            bool exists = await dbContext.RoleButtonLinks
                .AsQueryable()
                .AnyAsync(x => x.GuildID == guildId && x.ChannelID == channelId && x.MessageID == messageId && x.RoleID == roleId && x.EmoteString == emojiString)
                .ConfigureAwait(false);
            if (!exists)
            {
                var link = new RoleButtonLink { GuildID = guildId, ChannelID = channelId, MessageID = messageId, RoleID = roleId, EmoteString = emojiString };
                _ = dbContext.RoleButtonLinks.Add(link);
                _ = await dbContext.SaveChangesAsync().ConfigureAwait(false);
            }
            else
            {
                throw new ArgumentException("The specified link already exists");
            }
        }

        public async Task RemoveRoleButtonLinkAsync(ulong guildId, ulong channelId, ulong messageId, ulong roleId)
        {
            RoleButtonLink link = await dbContext.RoleButtonLinks
                .AsQueryable()
                .SingleOrDefaultAsync(x => x.GuildID == guildId && x.ChannelID == channelId && x.MessageID == messageId && x.RoleID == roleId)
                .ConfigureAwait(false);

            if (link == null)
            {
                throw new ArgumentException("Can't find specified role button link in database");
            }

            _ = dbContext.RoleButtonLinks.Remove(link);
            _ = await dbContext.SaveChangesAsync().ConfigureAwait(false);


            if (!discordClient.Guilds.TryGetValue(guildId, out DiscordGuild guild))
            {
                throw new ArgumentException("Invalid guild");
            }

            DiscordChannel channel = guild.GetChannel(channelId);
            if (channel == null)
            {
                throw new ArgumentException("Invalid channel");
            }

            DiscordMessage message = await channel.GetMessageAsync(messageId).ConfigureAwait(false);
            if (message == null)
            {
                throw new ArgumentException("Invalid message");
            }

            DiscordEmoji emoji = guild.Emojis.Values.FirstOrDefault(x => link.EmoteString.Contains(x.Name, StringComparison.Ordinal)) ?? DiscordEmoji.FromName(discordClient, link.EmoteString);
            if (emoji == null)
            {
                throw new ArgumentException("invalid emoji");
            }

            await message.DeleteReactionsEmojiAsync(emoji).ConfigureAwait(false);
        }

        public async Task RemoveAllRoleButtonLinksAsync(ulong guildId)
        {
            List<RoleButtonLink> links = await dbContext.RoleButtonLinks
                .AsQueryable()
                .Where(x => x.GuildID == guildId)
                .ToListAsync()
                .ConfigureAwait(false);
            dbContext.RoleButtonLinks.RemoveRange(links);
            _ = await dbContext.SaveChangesAsync().ConfigureAwait(false);
        }

        public async Task<bool> ExistsAsync(ulong guildID, ulong channelId, ulong messageID, ulong roleID, string emoteString = "")
        {
            List<RoleButtonLink> links = await dbContext.RoleButtonLinks
                .AsQueryable()
                .Where(x => x.GuildID == guildID && x.ChannelID == channelId && x.MessageID == messageID && x.RoleID == roleID)
                .ToListAsync()
                .ConfigureAwait(false);
            if (!emoteString.IsEmptyOrWhiteSpace())
            {
                links = links?.Where(x => x.EmoteString == emoteString).ToList();
            }
            return links?.Count > 0;
        }

        public async Task<string> ListAllAsync(ulong guildID)
        {
            List<RoleButtonLink> links = await dbContext.RoleButtonLinks
                .AsQueryable()
                .Where(x => x.GuildID == guildID)
                .ToListAsync()
                .ConfigureAwait(false);
            if (links == null || links.Count < 1)
            {
                return "";
            }

            var sb = new StringBuilder();
            foreach (RoleButtonLink link in links)
            {
                if (discordClient.Guilds.TryGetValue(link.GuildID, out DiscordGuild guild)
                    && guild.GetChannel(link.ChannelID) is DiscordChannel channel
                    && guild.Roles.TryGetValue(link.RoleID, out DiscordRole role)
                    && (await channel.GetMessageAsync(link.MessageID).ConfigureAwait(false)) is DiscordMessage message)
                {
                    _ = sb.AppendLine($"Message Id: [{link.MessageID}]({message.JumpLink}), Role: {role.Name}, Reaction: {link.EmoteString}");
                }
            }
            return sb.ToString();
        }

        private Task DiscordClient_MessageReactionAdded(MessageReactionAddEventArgs e)
            => AddOrRemoveRoleAsync(AddOrRemove.Add, e.Message, e.Channel, e.User, e.Emoji);

        private Task DiscordClient_MessageReactionRemoved(MessageReactionRemoveEventArgs e)
            => AddOrRemoveRoleAsync(AddOrRemove.Remove, e.Message, e.Channel, e.User, e.Emoji);


        private async Task AddOrRemoveRoleAsync(AddOrRemove action, DiscordMessage message, DiscordChannel channel, DiscordUser reactionUser, DiscordEmoji reactionEmoji)
        {
            if (channel == null)
            {
                logger.LogDebug($"Error in {nameof(AddOrRemoveRoleAsync)} of {nameof(RoleButtonService)} - Channel was null");
                return;
            }

            if (reactionUser == null)
            {
                logger.LogDebug($"Error in {nameof(AddOrRemoveRoleAsync)} of {nameof(RoleButtonService)} - No user was specified in the reaction object");
                return;
            }

            if (message == null)
            {
                logger.LogDebug($"Error in {nameof(AddOrRemoveRoleAsync)} of {nameof(RoleButtonService)} - Could not get the underlying message");
                return;
            }

            if (message.Channel.Type != ChannelType.Text)
            {
                logger.LogDebug($"Error in {nameof(AddOrRemoveRoleAsync)} of {nameof(RoleButtonService)} - message was not from a text channel");
                return;
            }

            DiscordGuild guild = message.Channel.Guild;
            if (guild == null)
            {
                logger.LogDebug($"Error in {nameof(AddOrRemoveRoleAsync)} of {nameof(RoleButtonService)} - Guild was null");
                return;
            }

            if (reactionUser.IsBot)
            {
                logger.LogDebug($"Error in {nameof(AddOrRemoveRoleAsync)} of {nameof(RoleButtonService)} - Reaction was triggered by a bot");
                return;
            }

            RoleButtonLink match = await dbContext.RoleButtonLinks
                .AsQueryable()
                .SingleOrDefaultAsync(x => x.GuildID == guild.Id && x.ChannelID == message.Channel.Id && x.MessageID == message.Id && x.EmoteString == reactionEmoji.ToString())
                .ConfigureAwait(false);
            if (match != null)
            {
                DiscordRole role = guild.GetRole(match.RoleID);
                DiscordMember gUser = await guild.GetMemberAsync(reactionUser.Id).ConfigureAwait(false);
                if (action == AddOrRemove.Add)
                {
                    await gUser.GrantRoleAsync(role).ConfigureAwait(false);
                    _ = await gUser.SendMessageAsync($"Role {role.Name} added").ConfigureAwait(false);
                }
                else
                {
                    await gUser.RevokeRoleAsync(role).ConfigureAwait(false);
                    _ = await gUser.SendMessageAsync($"Role {role.Name} removed").ConfigureAwait(false);
                }
            }
            else if (await dbContext.RoleButtonLinks.AsQueryable().AnyAsync(x => x.MessageID == message.Id).ConfigureAwait(false))
            {
                // Remove all new reactions that were not added by Bot
                await message.DeleteReactionAsync(reactionEmoji, reactionUser).ConfigureAwait(false);
            }
        }

        private enum AddOrRemove
        {
            Add,
            Remove
        }
    }
}