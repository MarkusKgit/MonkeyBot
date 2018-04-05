using Discord;
using Discord.WebSocket;
using dokas.FluentStrings;
using Microsoft.Extensions.DependencyInjection;
using MonkeyBot.Services.Common.GameSubscription;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MonkeyBot.Services.Implementations
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
            if (before.Game.HasValue && after.Game.HasValue && after.Game.Value.Name != before.Game.Value.Name)
                joinedGame = after.Game.Value.Name;
            if (!before.Game.HasValue && after.Game.HasValue)
                joinedGame = after.Game.Value.Name;
            if (joinedGame.IsEmpty())
                return;
            var gameSubscriptions = await GetGameSubscriptionsAsync();
            if (gameSubscriptions == null)
                return;
            foreach (var subscription in gameSubscriptions)
            {
                if (subscription == null)
                    continue;
                if (!joinedGame.ToLower().Contains(subscription.GameName.ToLower())) // Skip if user is not subscribed to game
                    continue;
                if (subscription.UserId == after.Id) // Don't message because of own game join
                    continue;
                var subscribedGuild = client.GetGuild(subscription.GuildId);
                if (subscribedGuild?.GetUser(after.Id) == null) // No message if in different Guild
                    continue;
                var subscribedUser = client.GetUser(subscription.UserId);
                if (subscribedUser == null)
                    continue;
                await subscribedUser.SendMessageAsync($"{after.Username} has launched {joinedGame}!");
            }
        }

        public async Task AddSubscriptionAsync(string gameName, ulong guildId, ulong userId)
        {
            var gameSubscription = new GameSubscription(guildId, userId, gameName);
            using (var uow = dbService.UnitOfWork)
            {
                await uow.GameSubscriptions.AddOrUpdateAsync(gameSubscription);
                await uow.CompleteAsync();
            }
        }

        public async Task RemoveSubscriptionAsync(string gameName, ulong guildId, ulong userId)
        {
            var subscriptions = await GetGameSubscriptionsAsync();
            var subscriptionToRemove = subscriptions.FirstOrDefault(x => x.GameName.ToLower().Contains(gameName.ToLower()) && x.GuildId == guildId && x.UserId == userId);
            if (subscriptionToRemove == null)
                throw new ArgumentException("The specified subscription does not exist");
            using (var uow = dbService.UnitOfWork)
            {
                await uow.GameSubscriptions.RemoveAsync(subscriptionToRemove);
                await uow.CompleteAsync();
            }
        }

        private async Task<List<GameSubscription>> GetGameSubscriptionsAsync()
        {
            using (var uow = dbService.UnitOfWork)
            {
                return await uow.GameSubscriptions.GetAllAsync();
            }
        }
    }
}