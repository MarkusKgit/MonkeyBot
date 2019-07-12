using Discord;
using Discord.WebSocket;
using dokas.FluentStrings;
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
        }

        public void Initialize()
        {
            discordClient.ReactionAdded += DiscordClient_ReactionAddedAsync;
            discordClient.ReactionRemoved += DiscordClient_ReactionRemovedAsync;
        }

        public async Task AddRoleButtonLinkAsync(ulong guildId, ulong messageId, ulong roleId, string emoteString)
        {
            var guild = discordClient.GetGuild(guildId);
            if (guild == null)
                return;
            var msg = await GetMessageAsync(guild, messageId).ConfigureAwait(false);
            if (msg == null)
                return;
            IEmote emote = guild.Emotes.FirstOrDefault(x => emoteString.Contains(x.Name, StringComparison.Ordinal)) ?? new Emoji(emoteString) as IEmote;
            if (emote == null)
                return;
            if (msg.Reactions.Count(x => x.Key == emote) < 1)
                await msg.AddReactionAsync(emote).ConfigureAwait(false);
            var link = new RoleButtonLink(guildId, messageId, roleId, emoteString);
            using (var uow = dbService.UnitOfWork)
            {
                await uow.RoleButtonLinks.AddOrUpdateAsync(link).ConfigureAwait(false);
                await uow.CompleteAsync().ConfigureAwait(false);
            }
        }

        public async Task RemoveRoleButtonLinkAsync(ulong guildId, ulong messageId, ulong roleId)
        {
            RoleButtonLink link;
            using (var uow = dbService.UnitOfWork)
            {
                link = (await uow.RoleButtonLinks.GetAllForGuildAsync(guildId, x => x.MessageId == messageId && x.RoleId == roleId).ConfigureAwait(false)).SingleOrDefault();
            }
            if (link == null)
                return;
            using (var uow = dbService.UnitOfWork)
            {
                await uow.RoleButtonLinks.RemoveAsync(link).ConfigureAwait(false);
                await uow.CompleteAsync().ConfigureAwait(false);
            }
            var guild = discordClient.GetGuild(guildId);
            if (guild == null)
                return;
            var msg = await GetMessageAsync(guild, messageId).ConfigureAwait(false);
            if (msg == null)
                return;
            IEmote emote = guild.Emotes.FirstOrDefault(x => link.EmoteString.Contains(x.Name, StringComparison.Ordinal)) ?? new Emoji(link.EmoteString) as IEmote;
            if (emote == null)
                return;
            var reactedUsers = msg.GetReactionUsersAsync(emote, 100);
            await reactedUsers.ForEachAsync(async users =>
            {
                foreach (var user in users)
                {
                    await msg.RemoveReactionAsync(emote, user).ConfigureAwait(false);
                }
            }).ConfigureAwait(false);
        }

        public async Task RemoveAllRoleButtonLinksAsync(ulong guildId)
        {
            using (var uow = dbService.UnitOfWork)
            {
                var links = await uow.RoleButtonLinks.GetAllForGuildAsync(guildId).ConfigureAwait(false);
                foreach (var link in links)
                {
                    await uow.RoleButtonLinks.RemoveAsync(link).ConfigureAwait(false);
                }
                await uow.CompleteAsync().ConfigureAwait(false);
            }
        }

        public async Task<bool> ExistsAsync(ulong guildId, ulong messageId, ulong roleId, string emoteString = "")
        {
            bool exists;
            using (var uow = dbService.UnitOfWork)
            {
                var links = await uow.RoleButtonLinks.GetAllForGuildAsync(guildId, x => x.MessageId == messageId && x.RoleId == roleId).ConfigureAwait(false);
                if (!emoteString.IsEmpty().OrWhiteSpace())
                    links = links?.Where(x => x.EmoteString == emoteString).ToList();
                exists = links?.Count() > 0;
            }
            return exists;
        }

        public async Task<string> ListAllAsync(ulong guildId)
        {
            List<RoleButtonLink> links;
            using (var uow = dbService.UnitOfWork)
            {
                links = await uow.RoleButtonLinks.GetAllForGuildAsync(guildId).ConfigureAwait(false);
            }
            if (links == null || links.Count() < 1)
                return "";
            var sb = new StringBuilder();
            foreach (var link in links)
            {
                var guild = discordClient.GetGuild(link.GuildId);
                var role = guild.GetRole(link.RoleId);
                sb.AppendLine($"Message Id: {link.MessageId} Role: {role.Name} Reaction: {link.EmoteString}");
            }
            return sb.ToString();
        }

        private async Task DiscordClient_ReactionRemovedAsync(Cacheable<IUserMessage, ulong> arg1, ISocketMessageChannel arg2, SocketReaction arg3)
        {
            await AddOrRemoveRoleAsync(AddOrRemove.Remove, arg1, arg2, arg3).ConfigureAwait(false);
        }

        private async Task DiscordClient_ReactionAddedAsync(Cacheable<IUserMessage, ulong> arg1, ISocketMessageChannel arg2, SocketReaction arg3)
        {
            await AddOrRemoveRoleAsync(AddOrRemove.Add, arg1, arg2, arg3).ConfigureAwait(false);
        }

        private async Task AddOrRemoveRoleAsync(AddOrRemove action, Cacheable<IUserMessage, ulong> arg1, ISocketMessageChannel arg2, SocketReaction arg3)
        {
            if (arg2 == null || !arg3.User.IsSpecified)
                return;
            var msg = arg1.HasValue ? arg1.Value : await arg1.GetOrDownloadAsync().ConfigureAwait(false);

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
            List<RoleButtonLink> buttonLinks = await GetRoleButtonLinksAsync().ConfigureAwait(false);
            var match = buttonLinks.SingleOrDefault(x => x.GuildId == guild.Id && x.MessageId == msg.Id && x.EmoteString == emote.ToString());
            if (match != null)
            {
                var role = guild.GetRole(match.RoleId);
                var gUser = await guild.GetUserAsync(user.Id).ConfigureAwait(false);
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
            else if (buttonLinks.Count(x => x.MessageId == msg.Id) > 0) // Remove all new reactions that were not added by Bot
            {
                await msg.RemoveReactionAsync(emote, user).ConfigureAwait(false);
            }
        }

        private async static Task<IUserMessage> GetMessageAsync(SocketGuild guild, ulong messageId)
        {
            foreach (var tc in guild.TextChannels)
            {
                var msg = await tc.GetMessageAsync(messageId).ConfigureAwait(false);
                if (msg != null && msg is IUserMessage userMsg)
                {
                    return userMsg;
                }
            }
            return null;
        }

        private async Task<List<RoleButtonLink>> GetRoleButtonLinksAsync()
        {
            List<RoleButtonLink> links;
            using (var uow = dbService.UnitOfWork)
            {
                links = await uow.RoleButtonLinks.GetAllAsync().ConfigureAwait(false);
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