using Discord;
using Discord.WebSocket;
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
        private readonly DiscordSocketClient discordClient;

        private readonly MonkeyDBContext dbContext;

        private readonly ILogger<RoleButtonService> logger;

        public RoleButtonService(DiscordSocketClient discordClient, MonkeyDBContext dbContext, ILogger<RoleButtonService> logger)
        {
            this.discordClient = discordClient;
            this.dbContext = dbContext;
            this.logger = logger;
        }

        public void Initialize()
        {
            discordClient.ReactionAdded += DiscordClient_ReactionAddedAsync;
            discordClient.ReactionRemoved += DiscordClient_ReactionRemovedAsync;
        }

        public async Task AddRoleButtonLinkAsync(ulong guildID, ulong messageID, ulong roleID, string emoteString)
        {
            SocketGuild guild = discordClient.GetGuild(guildID);
            if (guild == null)
            {
                return;
            }

            IUserMessage msg = await GetMessageAsync(guild, messageID).ConfigureAwait(false);
            if (msg == null)
            {
                return;
            }
            IEmote emote = guild.Emotes.FirstOrDefault(x => emoteString.Contains(x.Name, StringComparison.Ordinal))
                ?? new Emoji(emoteString) as IEmote;
            if (emote == null)
            {
                return;
            }

            if (!msg.Reactions.Any(x => x.Key == emote))
            {
                await msg.AddReactionAsync(emote).ConfigureAwait(false);
            }

            bool exists = await dbContext.RoleButtonLinks.AnyAsync(x => x.GuildID == guildID && x.MessageID == messageID && x.RoleID == roleID && x.EmoteString == emoteString).ConfigureAwait(false);
            if (!exists)
            {
                var link = new RoleButtonLink { GuildID = guildID, MessageID = messageID, RoleID = roleID, EmoteString = emoteString };
                _ = dbContext.RoleButtonLinks.Add(link);
                _ = await dbContext.SaveChangesAsync().ConfigureAwait(false);
            }
            else
            {
                throw new ArgumentException("The specified link already exists");
            }
        }

        public async Task RemoveRoleButtonLinkAsync(ulong guildID, ulong messageID, ulong roleID)
        {
            RoleButtonLink link = await dbContext.RoleButtonLinks.SingleOrDefaultAsync(x => x.GuildID == guildID
                                                                                            && x.MessageID == messageID
                                                                                            && x.RoleID == roleID).ConfigureAwait(false);
            if (link == null)
            {
                return;
            }

            _ = dbContext.RoleButtonLinks.Remove(link);
            _ = await dbContext.SaveChangesAsync().ConfigureAwait(false);
            SocketGuild guild = discordClient.GetGuild(guildID);
            if (guild == null)
            {
                return;
            }

            IUserMessage msg = await GetMessageAsync(guild, messageID).ConfigureAwait(false);
            if (msg == null)
            {
                return;
            }

            IEmote emote = guild.Emotes.FirstOrDefault(x => link.EmoteString.Contains(x.Name, StringComparison.Ordinal)) ?? new Emoji(link.EmoteString) as IEmote;
            if (emote == null)
            {
                return;
            }

            //TODO: change to explicit type once Discord.Net dependency on IAsyncEnumerable is fixed
            var reactedUsers = msg.GetReactionUsersAsync(emote, 100);
            await reactedUsers.ForEachAsync(async users =>
            {
                foreach (IUser user in users)
                {
                    await msg.RemoveReactionAsync(emote, user).ConfigureAwait(false);
                }
            }).ConfigureAwait(false);
        }

        public async Task RemoveAllRoleButtonLinksAsync(ulong guildID)
        {
            List<RoleButtonLink> links = await dbContext.RoleButtonLinks.Where(x => x.GuildID == guildID).ToListAsync().ConfigureAwait(false);
            dbContext.RoleButtonLinks.RemoveRange(links);
            _ = await dbContext.SaveChangesAsync().ConfigureAwait(false);
        }

        public async Task<bool> ExistsAsync(ulong guildID, ulong messageID, ulong roleID, string emoteString = "")
        {
            List<RoleButtonLink> links = await dbContext.RoleButtonLinks.Where(x => x.GuildID == guildID && x.MessageID == messageID && x.RoleID == roleID).ToListAsync().ConfigureAwait(false);
            if (!emoteString.IsEmptyOrWhiteSpace())
            {
                links = links?.Where(x => x.EmoteString == emoteString).ToList();
            }
            return links?.Count > 0;
        }

        public async Task<string> ListAllAsync(ulong guildID)
        {
            List<RoleButtonLink> links = await dbContext.RoleButtonLinks.Where(x => x.GuildID == guildID).ToListAsync().ConfigureAwait(false);
            if (links == null || links.Count < 1)
            {
                return "";
            }

            var sb = new StringBuilder();
            foreach (RoleButtonLink link in links)
            {
                SocketGuild guild = discordClient.GetGuild(link.GuildID);
                SocketRole role = guild.GetRole(link.RoleID);
                _ = sb.AppendLine($"Message Id: {link.MessageID} Role: {role.Name} Reaction: {link.EmoteString}");
            }
            return sb.ToString();
        }

        private Task DiscordClient_ReactionRemovedAsync(Cacheable<IUserMessage, ulong> cachedMessage, ISocketMessageChannel channel, SocketReaction reaction)
            => AddOrRemoveRoleAsync(AddOrRemove.Remove, cachedMessage, channel, reaction);

        private Task DiscordClient_ReactionAddedAsync(Cacheable<IUserMessage, ulong> cachedMessage, ISocketMessageChannel channel, SocketReaction reaction)
            => AddOrRemoveRoleAsync(AddOrRemove.Add, cachedMessage, channel, reaction);

        private async Task AddOrRemoveRoleAsync(AddOrRemove action, Cacheable<IUserMessage, ulong> cachedMessage, ISocketMessageChannel channel, SocketReaction reaction)
        {
            if (channel == null)
            {
                logger.LogDebug($"Error in {nameof(AddOrRemoveRoleAsync)} of {nameof(RoleButtonService)} - Channel was null");
                return;
            }

            if (reaction == null)
            {
                logger.LogDebug($"Error in {nameof(AddOrRemoveRoleAsync)} of {nameof(RoleButtonService)} - Reaction was null");
                return;
            }

            IEmote emote = reaction.Emote;

            if (!reaction.User.IsSpecified)
            {
                logger.LogDebug($"Error in {nameof(AddOrRemoveRoleAsync)} of {nameof(RoleButtonService)} - No user was specified in the reaction object");
                return;
            }

            IUser user = reaction.User.Value;

            IUserMessage msg = cachedMessage.HasValue ? cachedMessage.Value : await cachedMessage.GetOrDownloadAsync().ConfigureAwait(false);
            if (msg == null)
            {
                logger.LogDebug($"Error in {nameof(AddOrRemoveRoleAsync)} of {nameof(RoleButtonService)} - Could not get the underlying message");
                return;
            }

            if (!(msg.Channel is ITextChannel textChannel))
            {
                logger.LogDebug($"Error in {nameof(AddOrRemoveRoleAsync)} of {nameof(RoleButtonService)} - message was not from a text channel");
                return;
            }

            IGuild guild = textChannel.Guild;
            if (guild == null)
            {
                logger.LogDebug($"Error in {nameof(AddOrRemoveRoleAsync)} of {nameof(RoleButtonService)} - Guild was null");
                return;
            }

            if (user.IsBot)
            {
                logger.LogDebug($"Error in {nameof(AddOrRemoveRoleAsync)} of {nameof(RoleButtonService)} - Reaction was triggered by a bot");
                return;
            }

            RoleButtonLink match = await dbContext.RoleButtonLinks.SingleOrDefaultAsync(x => x.GuildID == guild.Id
                                                                                             && x.MessageID == msg.Id
                                                                                             && x.EmoteString == emote.ToString()).ConfigureAwait(false);
            if (match != null)
            {
                IRole role = guild.GetRole(match.RoleID);
                IGuildUser gUser = await guild.GetUserAsync(user.Id).ConfigureAwait(false);
                if (action == AddOrRemove.Add)
                {
                    await gUser.AddRoleAsync(role).ConfigureAwait(false);
                    _ = await gUser.SendMessageAsync($"Role {role.Name} added").ConfigureAwait(false);
                }
                else
                {
                    await gUser.RemoveRoleAsync(role).ConfigureAwait(false);
                    _ = await gUser.SendMessageAsync($"Role {role.Name} removed").ConfigureAwait(false);
                }
            }
            else if (await dbContext.RoleButtonLinks.AnyAsync(x => x.MessageID == msg.Id).ConfigureAwait(false)) // Remove all new reactions that were not added by Bot
            {
                await msg.RemoveReactionAsync(emote, user).ConfigureAwait(false);
            }
        }

        private async static Task<IUserMessage> GetMessageAsync(SocketGuild guild, ulong messageId)
        {
            foreach (SocketTextChannel tc in guild.TextChannels)
            {
                IMessage msg = await tc.GetMessageAsync(messageId).ConfigureAwait(false);
                if (msg != null && msg is IUserMessage userMsg)
                {
                    return userMsg;
                }
            }
            return null;
        }

        private enum AddOrRemove
        {
            Add,
            Remove
        }
    }
}