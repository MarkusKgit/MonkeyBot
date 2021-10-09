﻿using DSharpPlus;
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
        private readonly DiscordClient _discordClient;
        private readonly MonkeyDBContext _dbContext;
        private readonly ILogger<RoleButtonService> _logger;

        public RoleButtonService(DiscordClient discordClient, MonkeyDBContext dbContext, ILogger<RoleButtonService> logger)
        {
            _discordClient = discordClient;
            _dbContext = dbContext;
            _logger = logger;
        }

        //TODO: Convert this to dropdown buttons

        public void Initialize()
        {
            _discordClient.MessageReactionAdded += DiscordClient_MessageReactionAdded;
            _discordClient.MessageReactionRemoved += DiscordClient_MessageReactionRemoved;

            _discordClient.ComponentInteractionCreated += DiscordClient_ComponentInteractionCreated;
        }

        public async Task AddRoleSelectorComponentAsync(ulong guildId, ulong channelId, ulong messageId, DiscordSelectComponent roleSelectorComponent)
        {
            if (!_discordClient.Guilds.TryGetValue(guildId, out DiscordGuild guild))
            {
                throw new ArgumentException("Invalid guild");
            }

            DiscordChannel channel = guild.GetChannel(channelId);
            if (channel == null)
            {
                throw new ArgumentException("Invalid channel");
            }

            DiscordMessage message = await channel.GetMessageAsync(messageId);
            if (message == null)
            {
                throw new ArgumentException("Invalid message");
            }

            var messageBuilder = new DiscordMessageBuilder().WithContent("Please use this to assign yourself any role").AddComponents(roleSelectorComponent);
            await message.RespondAsync(messageBuilder);

            bool exists = await _dbContext.MessageComponentLinks
                .AsQueryable()
                .AnyAsync(x => x.GuildID == guildId && x.ChannelID == channelId && x.MessageID == messageId && x.ComponentId == roleSelectorComponent.CustomId)
                ;

            if (!exists)
            {
                var link = new MessageComponentLink { GuildID = guildId, ChannelID = channelId, MessageID = messageId, ComponentId = roleSelectorComponent.CustomId };
                _dbContext.MessageComponentLinks.Add(link);
                await _dbContext.SaveChangesAsync();
            }
            else
            {
                throw new ArgumentException("The specified link already exists");
            }
        }

        public async Task AddRoleButtonLinkAsync(ulong guildId, ulong channelId, ulong messageId, ulong roleId, string emojiString)
        {
            if (!_discordClient.Guilds.TryGetValue(guildId, out DiscordGuild guild))
            {
                throw new ArgumentException("Invalid guild");
            }

            DiscordChannel channel = guild.GetChannel(channelId);
            if (channel == null)
            {
                throw new ArgumentException("Invalid channel");
            }

            DiscordMessage message = await channel.GetMessageAsync(messageId);
            if (message == null)
            {
                throw new ArgumentException("Invalid message");
            }

            DiscordEmoji emoji = guild.Emojis.Values.FirstOrDefault(x => emojiString.Contains(x.Name, StringComparison.Ordinal)) ?? DiscordEmoji.FromName(_discordClient, emojiString);
            if (emoji == null)
            {
                throw new ArgumentException("invalid emoji");
            }

            if (!message.Reactions.Any(r => r.Emoji == emoji))
            {
                await message.CreateReactionAsync(emoji);
            }

            bool exists = await _dbContext.RoleButtonLinks
                .AsQueryable()
                .AnyAsync(x => x.GuildID == guildId && x.ChannelID == channelId && x.MessageID == messageId && x.RoleID == roleId && x.EmoteString == emojiString)
                ;
            if (!exists)
            {
                var link = new RoleButtonLink { GuildID = guildId, ChannelID = channelId, MessageID = messageId, RoleID = roleId, EmoteString = emojiString };
                _dbContext.RoleButtonLinks.Add(link);
                await _dbContext.SaveChangesAsync();
            }
            else
            {
                throw new ArgumentException("The specified link already exists");
            }
        }

        public async Task RemoveRoleButtonLinkAsync(ulong guildId, ulong channelId, ulong messageId, ulong roleId)
        {
            RoleButtonLink link = await _dbContext.RoleButtonLinks
                .AsQueryable()
                .SingleOrDefaultAsync(x => x.GuildID == guildId && x.ChannelID == channelId && x.MessageID == messageId && x.RoleID == roleId)
                ;

            if (link == null)
            {
                throw new ArgumentException("Can't find specified role button link in database");
            }

            _dbContext.RoleButtonLinks.Remove(link);
            await _dbContext.SaveChangesAsync();


            if (!_discordClient.Guilds.TryGetValue(guildId, out DiscordGuild guild))
            {
                throw new ArgumentException("Invalid guild");
            }

            DiscordChannel channel = guild.GetChannel(channelId);
            if (channel == null)
            {
                throw new ArgumentException("Invalid channel");
            }

            DiscordMessage message = await channel.GetMessageAsync(messageId);
            if (message == null)
            {
                throw new ArgumentException("Invalid message");
            }

            DiscordEmoji emoji = guild.Emojis.Values.FirstOrDefault(x => link.EmoteString.Contains(x.Name, StringComparison.Ordinal)) ?? DiscordEmoji.FromName(_discordClient, link.EmoteString);
            if (emoji == null)
            {
                throw new ArgumentException("invalid emoji");
            }

            await message.DeleteReactionsEmojiAsync(emoji);
        }

        public async Task RemoveAllRoleButtonLinksAsync(ulong guildId)
        {
            List<RoleButtonLink> links = await _dbContext.RoleButtonLinks
                .AsQueryable()
                .Where(x => x.GuildID == guildId)
                .ToListAsync()
                ;
            _dbContext.RoleButtonLinks.RemoveRange(links);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<bool> ExistsAsync(ulong guildID, ulong channelId, ulong messageID, ulong roleID, string emoteString = "")
        {
            List<RoleButtonLink> links = await _dbContext.RoleButtonLinks
                .AsQueryable()
                .Where(x => x.GuildID == guildID && x.ChannelID == channelId && x.MessageID == messageID && x.RoleID == roleID)
                .ToListAsync()
                ;
            if (!emoteString.IsEmptyOrWhiteSpace())
            {
                links = links?.Where(x => x.EmoteString == emoteString).ToList();
            }
            return links?.Count > 0;
        }

        public async Task<bool> ExistsAsync(ulong guildID, ulong channelId, ulong messageID)
        {
            List<MessageComponentLink> links = await _dbContext.MessageComponentLinks
                .AsQueryable()
                .Where(x => x.GuildID == guildID && x.ChannelID == channelId && x.MessageID == messageID)
                .ToListAsync();
            return links?.Count > 0;
        }

        public async Task<string> ListAllAsync(ulong guildID)
        {
            List<RoleButtonLink> links = await _dbContext.RoleButtonLinks
                .AsQueryable()
                .Where(x => x.GuildID == guildID)
                .ToListAsync()
                ;
            if (links == null || links.Count < 1)
            {
                return "";
            }

            var sb = new StringBuilder();
            foreach (RoleButtonLink link in links)
            {
                if (_discordClient.Guilds.TryGetValue(link.GuildID, out DiscordGuild guild)
                    && guild.GetChannel(link.ChannelID) is DiscordChannel channel
                    && guild.Roles.TryGetValue(link.RoleID, out DiscordRole role)
                    && (await channel.GetMessageAsync(link.MessageID)) is DiscordMessage message)
                {
                    sb.AppendLine($"Message Id: [{link.MessageID}]({message.JumpLink}), Role: {role.Name}, Reaction: {link.EmoteString}");
                }
            }
            return sb.ToString();
        }

        private Task DiscordClient_MessageReactionAdded(DiscordClient client, MessageReactionAddEventArgs e)
            => AddOrRemoveRoleAsync(AddOrRemove.Add, e.Message, e.Channel, e.User, e.Emoji);

        private Task DiscordClient_MessageReactionRemoved(DiscordClient client, MessageReactionRemoveEventArgs e)
            => AddOrRemoveRoleAsync(AddOrRemove.Remove, e.Message, e.Channel, e.User, e.Emoji);

        private async Task DiscordClient_ComponentInteractionCreated(DiscordClient sender, ComponentInteractionCreateEventArgs e)
        {
            var interactionUser = e.Interaction.User;
            var guild = e.Guild;
            var channel = e.Interaction.Channel;
            var message = e.Message;

            MessageComponentLink match = await _dbContext.MessageComponentLinks
                .AsQueryable()
                .SingleOrDefaultAsync(x => x.GuildID == guild.Id && x.ChannelID == message.Channel.Id && x.ComponentId == e.Id);
            if (match is object && e.Values.Any())
            {
                var selectedRoleId = e.Values[0];
                await AssignRole(sender, channel, guild, interactionUser, selectedRoleId);
            }
            await e.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
        }

        private async Task AssignRole(DiscordClient client, DiscordChannel channel, DiscordGuild guild, DiscordUser interactionUser, string selectedRoleId)
        {
            if (interactionUser.IsBot)
            {
                _logger.LogDebug($"Error in {nameof(AssignRole)} of {nameof(RoleButtonService)} - Reaction was triggered by a bot");
                return;
            }

            var interactionMember = await guild.GetMemberAsync(interactionUser.Id);

            if (ulong.TryParse(selectedRoleId, out var roleId))
            {
                DiscordRole role = guild.GetRole(roleId);

                if (interactionMember.Roles.Contains(role))
                {
                    await client.SendMessageAsync(channel, $"{interactionMember.DisplayName} already has the role {role.Name}");
                    return;
                }

                await interactionMember.GrantRoleAsync(role);
                await client.SendMessageAsync(channel, $"Role {role.Name} assigned to user {interactionMember.DisplayName}");
            }
            else
            {
                _logger.LogDebug($"Error in {nameof(AssignRole)} of {nameof(RoleButtonService)} - Could not find the selected role");
                return;
            }
        }


        private async Task AddOrRemoveRoleAsync(AddOrRemove action, DiscordMessage message, DiscordChannel channel, DiscordUser reactionUser, DiscordEmoji reactionEmoji)
        {
            if (channel == null)
            {
                _logger.LogDebug($"Error in {nameof(AddOrRemoveRoleAsync)} of {nameof(RoleButtonService)} - Channel was null");
                return;
            }

            if (reactionUser == null)
            {
                _logger.LogDebug($"Error in {nameof(AddOrRemoveRoleAsync)} of {nameof(RoleButtonService)} - No user was specified in the reaction object");
                return;
            }

            if (message == null)
            {
                _logger.LogDebug($"Error in {nameof(AddOrRemoveRoleAsync)} of {nameof(RoleButtonService)} - Could not get the underlying message");
                return;
            }

            if (message.Channel.Type != ChannelType.Text)
            {
                _logger.LogDebug($"Error in {nameof(AddOrRemoveRoleAsync)} of {nameof(RoleButtonService)} - message was not from a text channel");
                return;
            }

            DiscordGuild guild = message.Channel.Guild;
            if (guild == null)
            {
                _logger.LogDebug($"Error in {nameof(AddOrRemoveRoleAsync)} of {nameof(RoleButtonService)} - Guild was null");
                return;
            }

            if (reactionUser.IsBot)
            {
                _logger.LogDebug($"Error in {nameof(AddOrRemoveRoleAsync)} of {nameof(RoleButtonService)} - Reaction was triggered by a bot");
                return;
            }

            RoleButtonLink match = await _dbContext.RoleButtonLinks
                .AsQueryable()
                .SingleOrDefaultAsync(x => x.GuildID == guild.Id && x.ChannelID == message.Channel.Id && x.MessageID == message.Id && x.EmoteString == reactionEmoji.ToString())
                ;
            if (match != null)
            {
                DiscordRole role = guild.GetRole(match.RoleID);
                DiscordMember gUser = await guild.GetMemberAsync(reactionUser.Id);
                if (action == AddOrRemove.Add)
                {
                    await gUser.GrantRoleAsync(role);
                    await gUser.SendMessageAsync($"Role {role.Name} added");
                }
                else
                {
                    await gUser.RevokeRoleAsync(role);
                    await gUser.SendMessageAsync($"Role {role.Name} removed");
                }
            }
            else if (await _dbContext.RoleButtonLinks.AsQueryable().AnyAsync(x => x.MessageID == message.Id))
            {
                // Remove all new reactions that were not added by Bot
                await message.DeleteReactionAsync(reactionEmoji, reactionUser);
            }
        }

        private enum AddOrRemove
        {
            Add,
            Remove
        }
    }
}