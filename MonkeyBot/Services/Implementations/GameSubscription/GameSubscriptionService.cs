using Discord;
using Discord.WebSocket;
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
            discordClient = client;
        }

        public void Initialize() => discordClient.GuildMemberUpdated += Client_GuildMemberUpdatedAsync;

        private async Task Client_GuildMemberUpdatedAsync(SocketUser before, SocketUser after)
        {
            string joinedGame = null;
            if ((before.Activity != null && after.Activity != null && after.Activity.Name != before.Activity.Name)
                || (before.Activity == null && after.Activity != null))
            {
                joinedGame = after.Activity.Name;
            }
            if (joinedGame.IsEmptyOrWhiteSpace())
            {
                return;
            }
            List<GameSubscription> gameSubscriptions = await dbContext.GameSubscriptions
                .AsQueryable()
                .Where(subscription => subscription != null
                                       && joinedGame.ToUpper().Contains(subscription.GameName.ToUpper())
                                       && subscription.UserID != after.Id)
                .ToListAsync()
                .ConfigureAwait(false);
            if (gameSubscriptions == null)
            {
                return;
            }

            foreach (GameSubscription subscription in gameSubscriptions)
            {
                SocketGuild subscribedGuild = discordClient.GetGuild(subscription.GuildID);
                if (subscribedGuild?.GetUser(after.Id) == null) // No message if the user starting the activity is in different Guild
                {
                    continue;
                }
                IUser subscribedUser = discordClient.GetUser(subscription.UserID);
                if (subscribedUser == null)
                {
                    continue;
                }

                _ = await subscribedUser.SendMessageAsync($"{after.Username} has launched {joinedGame}!").ConfigureAwait(false);
            }
        }
                
        public Task AddSubscriptionAsync(string gameName, ulong guildID, ulong userID)
        {
            if (dbContext.GameSubscriptions.Any(x => x.GameName.ToUpper().Contains(gameName.ToUpper()) && x.GuildID == guildID && x.UserID == userID))            
            {
                throw new ArgumentException("The user is already subscribed to that game");
            }

            var gameSubscription = new GameSubscription { GuildID = guildID, UserID = userID, GameName = gameName };
            _ = dbContext.GameSubscriptions.Add(gameSubscription);
            return dbContext.SaveChangesAsync();
        }

        public async Task RemoveSubscriptionAsync(string gameName, ulong guildID, ulong userID)
        {
            GameSubscription subscriptionToRemove = await dbContext.GameSubscriptions
                .AsQueryable()
                .FirstOrDefaultAsync(x => x.GameName.ToUpper().Contains(gameName.ToUpper())
                                          && x.GuildID == guildID
                                          && x.UserID == userID)
                .ConfigureAwait(false);
            if (subscriptionToRemove == null)
            {
                throw new ArgumentException("The specified subscription does not exist");
            }

            _ = dbContext.GameSubscriptions.Remove(subscriptionToRemove);
            _ = await dbContext.SaveChangesAsync().ConfigureAwait(false);
        }

        public async Task<IReadOnlyCollection<GameSubscription>> GetSubscriptionsForUser(ulong userID)
            => (await dbContext.GameSubscriptions.AsQueryable().Where(x => x.UserID == userID).ToListAsync().ConfigureAwait(false)).AsReadOnly();
    }
}