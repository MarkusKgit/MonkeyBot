using Discord.Commands;
using MonkeyBot.Common;
using MonkeyBot.Preconditions;
using MonkeyBot.Services;
using System;
using System.Threading.Tasks;

namespace MonkeyBot.Modules
{
    [Name("Game Subscriptions")]
    [MinPermissions(AccessLevel.User)]
    [RequireContext(ContextType.Guild)]
    public class GameSubscriptionModule : ModuleBase
    {
        private IGameSubscriptionService gameSubscriptionService;

        public GameSubscriptionModule(IGameSubscriptionService gameSubscriptionService)
        {
            this.gameSubscriptionService = gameSubscriptionService;
        }

        [Command("Subscribe")]
        [Remarks("Subscribes to the specified game to get a PM every time someone launches it")]
        public async Task SubscribeAsync([Summary("The name of the game to subscribe to.")] [Remainder] string gameName = null)
        {
            if (string.IsNullOrEmpty(gameName))
            {
                await ReplyAsync("You need to specify a game you wish to subscribe to!");
                return;
            }
            try
            {
                // Add the Subscription to the Service to activate it
                await gameSubscriptionService.AddSubscriptionAsync(gameName, Context.Guild.Id, Context.User.Id);
            }
            catch (Exception ex)
            {
                await ReplyAsync($"There was an error while adding the subscription:{Environment.NewLine}{ex.Message}");
                await Console.Out.WriteLineAsync(ex.Message);
            }
            await ReplyAsync($"You are now subscribed to {gameName}");
        }

        [Command("Unsubscribe")]
        [Remarks("Unsubscribes to the specified game")]
        public async Task UnsubscribeAsync([Summary("The name of the game to unsubscribe from.")] [Remainder] string gameName = null)
        {
            if (string.IsNullOrEmpty(gameName))
            {
                await ReplyAsync("You need to specify a game you wish to unsubscribe from!");
                return;
            }
            try
            {
                // Remove the subscription from the Service
                await gameSubscriptionService.RemoveSubscriptionAsync(gameName, Context.Guild.Id, Context.User.Id);
            }
            catch (Exception ex)
            {
                await ReplyAsync($"There was an error while trying to remove the subscription:{Environment.NewLine}{ex.Message}");
                await Console.Out.WriteLineAsync(ex.Message);
            }
            await ReplyAsync($"You are now unsubscribed from {gameName}");
        }
    }
}