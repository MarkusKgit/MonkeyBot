using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
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

        public RoleButtonService(DiscordSocketClient discordClient, MonkeyDBContext dbContext)
        {
            this.discordClient = discordClient;
            this.dbContext = dbContext;
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
                return;
            IUserMessage msg = await GetMessageAsync(guild, messageID).ConfigureAwait(false);
            if (msg == null)
                return;
            IEmote emote = guild.Emotes.FirstOrDefault(x => emoteString.Contains(x.Name, StringComparison.Ordinal)) ?? new Emoji(emoteString) as IEmote;
            if (emote == null)
                return;
            if (!msg.Reactions.Any(x => x.Key == emote))
                await msg.AddReactionAsync(emote).ConfigureAwait(false);

            bool exists = await dbContext.RoleButtonLinks.AnyAsync(x => x.GuildID == guildID && x.MessageID == messageID && x.RoleID == roleID && x.EmoteString == emoteString).ConfigureAwait(false);
            if (!exists)
            {
                var link = new RoleButtonLink { GuildID = guildID, MessageID = messageID, RoleID = roleID, EmoteString = emoteString };
                dbContext.RoleButtonLinks.Add(link);
                await dbContext.SaveChangesAsync().ConfigureAwait(false);
            }
            else
            {
                throw new ArgumentException("The specified link already exists");
            }
        }

        public async Task RemoveRoleButtonLinkAsync(ulong guildID, ulong messageID, ulong roleID)
        {
            RoleButtonLink link = await dbContext.RoleButtonLinks.SingleOrDefaultAsync(x => x.GuildID == guildID && x.MessageID == messageID && x.RoleID == roleID).ConfigureAwait(false);
            if (link == null)
                return;
            dbContext.RoleButtonLinks.Remove(link);
            await dbContext.SaveChangesAsync().ConfigureAwait(false);
            SocketGuild guild = discordClient.GetGuild(guildID);
            if (guild == null)
                return;
            IUserMessage msg = await GetMessageAsync(guild, messageID).ConfigureAwait(false);
            if (msg == null)
                return;
            IEmote emote = guild.Emotes.FirstOrDefault(x => link.EmoteString.Contains(x.Name, StringComparison.Ordinal)) ?? new Emoji(link.EmoteString) as IEmote;
            if (emote == null)
                return;
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
            await dbContext.SaveChangesAsync().ConfigureAwait(false);
        }

        public async Task<bool> ExistsAsync(ulong guildID, ulong messageID, ulong roleID, string emoteString = "")
        {
            List<RoleButtonLink> links = await dbContext.RoleButtonLinks.Where(x => x.GuildID == guildID && x.MessageID == messageID && x.RoleID == roleID).ToListAsync().ConfigureAwait(false);
            if (!emoteString.IsEmptyOrWhiteSpace())
                links = links?.Where(x => x.EmoteString == emoteString).ToList();
            return links?.Count > 0;
        }

        public async Task<string> ListAllAsync(ulong guildID)
        {
            List<RoleButtonLink> links = await dbContext.RoleButtonLinks.Where(x => x.GuildID == guildID).ToListAsync().ConfigureAwait(false);
            if (links == null || links.Count < 1)
                return "";
            var sb = new StringBuilder();
            foreach (RoleButtonLink link in links)
            {
                SocketGuild guild = discordClient.GetGuild(link.GuildID);
                SocketRole role = guild.GetRole(link.RoleID);
                sb.AppendLine($"Message Id: {link.MessageID} Role: {role.Name} Reaction: {link.EmoteString}");
            }
            return sb.ToString();
        }

        private Task DiscordClient_ReactionRemovedAsync(Cacheable<IUserMessage, ulong> cachedMessage, ISocketMessageChannel channel, SocketReaction reaction) 
            => AddOrRemoveRoleAsync(AddOrRemove.Remove, cachedMessage, channel, reaction);

        private Task DiscordClient_ReactionAddedAsync(Cacheable<IUserMessage, ulong> cachedMessage, ISocketMessageChannel channel, SocketReaction reaction) 
            => AddOrRemoveRoleAsync(AddOrRemove.Add, cachedMessage, channel, reaction);

        private async Task AddOrRemoveRoleAsync(AddOrRemove action, Cacheable<IUserMessage, ulong> cachedMessage, ISocketMessageChannel channel, SocketReaction reaction)
        {
            if (channel == null || !reaction.User.IsSpecified)
                return;
            IUserMessage msg = cachedMessage.HasValue ? cachedMessage.Value : await cachedMessage.GetOrDownloadAsync().ConfigureAwait(false);

            if (!(msg.Channel is ITextChannel textChannel))
                return;
            IGuild guild = textChannel.Guild;
            if (guild == null)
                return;
            IUser user = reaction.User.Value;
            IEmote emote = reaction.Emote;
            if (user.IsBot)
                return;
            RoleButtonLink match = await dbContext.RoleButtonLinks.SingleOrDefaultAsync(x => x.GuildID == guild.Id && x.MessageID == msg.Id && x.EmoteString == emote.ToString()).ConfigureAwait(false);
            if (match != null)
            {
                IRole role = guild.GetRole(match.RoleID);
                IGuildUser gUser = await guild.GetUserAsync(user.Id).ConfigureAwait(false);
                if (action == AddOrRemove.Add)
                {
                    await gUser.AddRoleAsync(role).ConfigureAwait(false);
                    await gUser.SendMessageAsync($"Role {role.Name} added").ConfigureAwait(false);
                }
                else
                {
                    await gUser.RemoveRoleAsync(role).ConfigureAwait(false);
                    await gUser.SendMessageAsync($"Role {role.Name} removed").ConfigureAwait(false);
                }
            }
            else if ((await dbContext.RoleButtonLinks.CountAsync(x => x.MessageID == msg.Id).ConfigureAwait(false)) > 0) // Remove all new reactions that were not added by Bot
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