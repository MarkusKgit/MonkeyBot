using Discord;
using Discord.WebSocket;
using dokas.FluentStrings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MonkeyBot.Services
{
    public class GameSubscriptionService : IGameSubscriptionService
    {
        private readonly DbService dbService;
        private readonly DiscordSocketClient client;

        public GameSubscriptionService(DbService dbService, DiscordSocketClient client)
        {
            this.dbService = dbService;
            this.client = client;
        }

        public void Initialize()
        {
            client.GuildMemberUpdated += Client_GuildMemberUpdatedAsync;
        }

        private async Task Client_GuildMemberUpdatedAsync(SocketUser before, SocketUser after)
        {
            string joinedGame = null;
            if (before.Activity != null && after.Activity != null && after.Activity.Name != before.Activity.Name)
                joinedGame = after.Activity.Name;
            if (before.Activity == null && after.Activity != null)
                joinedGame = after.Activity.Name;
            if (joinedGame.IsEmpty())
                return;
            var gameSubscriptions = await GetGameSubscriptionsAsync().ConfigureAwait(false);
            if (gameSubscriptions == null)
                return;
            foreach (var subscription in gameSubscriptions)
            {
                if (subscription == null)
                    continue;
                if (!joinedGame.Contains(subscription.GameName, StringComparison.OrdinalIgnoreCase)) // Skip if user is not subscribed to game
                    continue;
                if (subscription.UserId == after.Id) // Don't message because of own game join
                    continue;
                var subscribedGuild = client.GetGuild(subscription.GuildId);
                if (subscribedGuild?.GetUser(after.Id) == null) // No message if in different Guild
                    continue;
                var subscribedUser = client.GetUser(subscription.UserId);
                if (subscribedUser == null)
                    continue;
                await subscribedUser.SendMessageAsync($"{after.Username} has launched {joinedGame}!").ConfigureAwait(false);
            }
        }

        public async Task AddSubscriptionAsync(string gameName, ulong guildId, ulong userId)
        {
            var gameSubscription = new GameSubscription(guildId, userId, gameName);
            using (var uow = dbService.UnitOfWork)
            {
                await uow.GameSubscriptions.AddOrUpdateAsync(gameSubscription).ConfigureAwait(false);
                await uow.CompleteAsync().ConfigureAwait(false);
            }
        }

        public async Task RemoveSubscriptionAsync(string gameName, ulong guildId, ulong userId)
        {
            var subscriptions = await GetGameSubscriptionsAsync().ConfigureAwait(false);
            var subscriptionToRemove = subscriptions.FirstOrDefault(x => x.GameName.Contains(gameName, StringComparison.OrdinalIgnoreCase) && x.GuildId == guildId && x.UserId == userId);
            if (subscriptionToRemove == null)
                throw new ArgumentException("The specified subscription does not exist");
            using (var uow = dbService.UnitOfWork)
            {
                await uow.GameSubscriptions.RemoveAsync(subscriptionToRemove).ConfigureAwait(false);
                await uow.CompleteAsync().ConfigureAwait(false);
            }
        }

        private async Task<List<GameSubscription>> GetGameSubscriptionsAsync()
        {
            using (var uow = dbService.UnitOfWork)
            {
                return await uow.GameSubscriptions.GetAllAsync().ConfigureAwait(false);
            }
        }
    }
}