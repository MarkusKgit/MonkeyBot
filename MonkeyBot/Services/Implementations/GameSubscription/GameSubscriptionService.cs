using Discord;
using Discord.WebSocket;
using dokas.FluentStrings;
using Microsoft.EntityFrameworkCore;
using MonkeyBot.Database;
using MonkeyBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MonkeyBot.Services
{
    public class GameSubscriptionService : IGameSubscriptionService
    {
        private readonly MonkeyDBContext dbContext;
        private readonly DiscordSocketClient discordClient;

        public GameSubscriptionService(MonkeyDBContext dbContext, DiscordSocketClient client)
        {
            this.dbContext = dbContext;
            this.discordClient = client;
        }

        public void Initialize()
        {
            discordClient.GuildMemberUpdated += Client_GuildMemberUpdatedAsync;
        }

        private async Task Client_GuildMemberUpdatedAsync(SocketUser before, SocketUser after)
        {
            string joinedGame = null;
            if (before.Activity != null && after.Activity != null && after.Activity.Name != before.Activity.Name)
                joinedGame = after.Activity.Name;
            if (before.Activity == null && after.Activity != null)
                joinedGame = after.Activity.Name;
            if (joinedGame.IsEmpty().OrWhiteSpace())
                return;
            var gameSubscriptions = await dbContext.GameSubscriptions.ToListAsync().ConfigureAwait(false);
            if (gameSubscriptions == null)
                return;
            foreach (var subscription in gameSubscriptions)
            {
                if (subscription == null)
                    continue;
                if (!joinedGame.Contains(subscription.GameName, StringComparison.OrdinalIgnoreCase)) // Skip if user is not subscribed to game
                    continue;
                if (subscription.UserID == after.Id) // Don't message because of own game join
                    continue;
                var subscribedGuild = discordClient.GetGuild(subscription.GuildID);
                if (subscribedGuild?.GetUser(after.Id) == null) // No message if in different Guild
                    continue;
                var subscribedUser = discordClient.GetUser(subscription.UserID);
                if (subscribedUser == null)
                    continue;
                await subscribedUser.SendMessageAsync($"{after.Username} has launched {joinedGame}!").ConfigureAwait(false);
            }
        }

        public async Task AddSubscriptionAsync(string gameName, ulong guildID, ulong userID)
        {
            if (dbContext.GameSubscriptions.Any(x => x.GameName.Contains(gameName, StringComparison.OrdinalIgnoreCase) && x.GuildID == guildID && x.UserID == userID))
                throw new ArgumentException("The user is already subscribed to that game");
            var gameSubscription = new GameSubscription { GuildID = guildID, UserID = userID, GameName = gameName };
            dbContext.GameSubscriptions.Add(gameSubscription);
            await dbContext.SaveChangesAsync().ConfigureAwait(false);
        }

        public async Task RemoveSubscriptionAsync(string gameName, ulong guildID, ulong userID)
        {
            var subscriptionToRemove = await dbContext
                .GameSubscriptions
                .FirstOrDefaultAsync(x => x.GameName.Contains(gameName, StringComparison.OrdinalIgnoreCase) && x.GuildID == guildID && x.UserID == userID)
                .ConfigureAwait(false);
            if (subscriptionToRemove == null)
                throw new ArgumentException("The specified subscription does not exist");
            dbContext.GameSubscriptions.Remove(subscriptionToRemove);
            await dbContext.SaveChangesAsync().ConfigureAwait(false);
        }

        public async Task<IReadOnlyCollection<GameSubscription>> GetSubscriptionsForUser(ulong userID)
        {
            return (await dbContext.GameSubscriptions.Where(x => x.UserID == userID).ToListAsync().ConfigureAwait(false)).AsReadOnly();
        }
    }
}