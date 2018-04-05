using Discord;
using Discord.WebSocket;
using MonkeyBot.Services.Common.RoleButtons;
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

        private readonly DbService dbService;

        public RoleButtonService(DiscordSocketClient discordClient, DbService dbService)
        {
            this.discordClient = discordClient;
            this.dbService = dbService;
            discordClient.ReactionAdded += DiscordClient_ReactionAddedAsync;
            discordClient.ReactionRemoved += DiscordClient_ReactionRemovedAsync;
        }

        public async Task AddRoleButtonLinkAsync(ulong guildId, ulong messageId, ulong roleId, string emoji)
        {
            var msg = await GetMessageAsync(guildId, messageId);
            if (msg.Reactions.Count(x => x.Key.Name == emoji) < 1)
                await msg.AddReactionAsync(new Emoji(emoji));

            var link = new RoleButtonLink(guildId, messageId, roleId, emoji);
            using (var uow = dbService.UnitOfWork)
            {
                await uow.RoleButtonLinks.AddOrUpdateAsync(link);
                await uow.CompleteAsync();
            }
        }

        public async Task RemoveRoleButtonLinkAsync(ulong guildId, ulong messageId, ulong roleId)
        {
            var links = await GetRoleButtonLinksForGuildAsync(guildId);
            var link = links.SingleOrDefault(x => x.MessageId == messageId && x.RoleId == roleId);
            if (link == null)
                return;
            var msg = await GetMessageAsync(guildId, messageId);
            var existingReactions = msg.Reactions.Where(x => x.Key.Name == link.Emote);
            foreach (var reaction in existingReactions)
            {
                await msg.RemoveReactionAsync(reaction.Key, null); // How to get the user here?
            }
            using (var uow = dbService.UnitOfWork)
            {
                await uow.RoleButtonLinks.RemoveAsync(link);
                await uow.CompleteAsync();
            }
        }

        public async Task RemoveAllRoleButtonLinksAsync(ulong guildId)
        {
            //TODO: remove all
        }

        public async Task<string> ListAllAsync(ulong guildId)
        {
            var links = await GetRoleButtonLinksForGuildAsync(guildId);
            var sb = new StringBuilder();
            foreach (var link in links)
            {
                var guild = discordClient.GetGuild(link.GuildId);
                var role = guild.GetRole(link.RoleId);
                sb.AppendLine($"Message Id: {link.MessageId} Role: {role.Name} Reaction: {link.Emote}");
            }
            return sb.ToString();
        }

        private async Task DiscordClient_ReactionRemovedAsync(Cacheable<IUserMessage, ulong> arg1, ISocketMessageChannel arg2, SocketReaction arg3)
        {
            await AddOrRemoveRoleAsync(AddOrRemove.Remove, arg1, arg2, arg3);
        }

        private async Task DiscordClient_ReactionAddedAsync(Cacheable<IUserMessage, ulong> arg1, ISocketMessageChannel arg2, SocketReaction arg3)
        {
            await AddOrRemoveRoleAsync(AddOrRemove.Add, arg1, arg2, arg3);
        }

        private async Task AddOrRemoveRoleAsync(AddOrRemove action, Cacheable<IUserMessage, ulong> arg1, ISocketMessageChannel arg2, SocketReaction arg3)
        {
            if (!arg1.HasValue || arg2 == null || !arg3.User.IsSpecified)
                return;
            var msg = arg1.Value;
            if (!(msg.Channel is ITextChannel textChannel))
                return;
            var guild = textChannel.Guild;
            if (guild == null)
                return;
            var user = arg3.User.Value;
            var channelID = arg2.Id;
            var emote = arg3.Emote;
            if (user.IsBot)
                return;
            List<RoleButtonLink> buttonLinks = await GetRoleButtonLinksAsync();
            var match = buttonLinks.SingleOrDefault(x => x.GuildId == guild.Id && x.MessageId == msg.Id && x.Emote == emote.Name);
            if (match != null)
            {
                var role = guild.GetRole(match.RoleId);
                var gUser = await guild.GetUserAsync(user.Id);
                if (action == AddOrRemove.Add)
                {
                    await gUser.AddRoleAsync(role);
                    await gUser.SendMessageAsync($"Role {role.Name} added");
                }
                else
                {
                    await gUser.RemoveRoleAsync(role);
                    await gUser.SendMessageAsync($"Role {role.Name} removed");
                }
            }
        }

        private async Task<IUserMessage> GetMessageAsync(ulong guildId, ulong messageId)
        {
            var guild = discordClient.GetGuild(guildId);
            foreach (var tc in guild.TextChannels)
            {
                var msg = await tc.GetMessageAsync(messageId);
                if (msg != null && msg is IUserMessage userMsg)
                {
                    return userMsg;
                }
            }
            return null;
        }

        private Task<List<RoleButtonLink>> GetRoleButtonLinksAsync()
        {
            //TODO: Implement DB query
            return new Task<List<RoleButtonLink>>(() => new List<RoleButtonLink>());
        }

        private async Task<List<RoleButtonLink>> GetRoleButtonLinksForGuildAsync(ulong guildId)
        {
            List<RoleButtonLink> links;
            using (var uow = dbService.UnitOfWork)
            {
                links = await uow.RoleButtonLinks.GetAllForGuildAsync(guildId);
            }
            return links;
        }

        private enum AddOrRemove
        {
            Add,
            Remove
        }
    }
}