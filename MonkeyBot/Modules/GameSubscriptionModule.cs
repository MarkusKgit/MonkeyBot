using Discord.Commands;
using dokas.FluentStrings;
using Microsoft.Extensions.Logging;
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
    public class GameSubscriptionModule : MonkeyModuleBase
    {
        private readonly IGameSubscriptionService gameSubscriptionService;
        private readonly ILogger<GameSubscriptionModule> logger;

        public GameSubscriptionModule(IGameSubscriptionService gameSubscriptionService, ILogger<GameSubscriptionModule> logger)
        {
            this.gameSubscriptionService = gameSubscriptionService;
            this.logger = logger;
        }

        [Command("Subscribe")]
        [Remarks("Subscribes to the specified game. You will get a private message every time someone launches it")]
        [Example("!Subscribe \"Battlefield 1\"")]
        public async Task SubscribeAsync([Summary("The name of the game to subscribe to.")] [Remainder] string gameName)
        {
            if (gameName.IsEmpty())
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
                logger.LogWarning(ex, "Error adding a game subscription");
            }
            await ReplyAsync($"You are now subscribed to {gameName}");
        }

        [Command("Unsubscribe")]
        [Remarks("Unsubscribes to the specified game")]
        [Example("!Unsubscribe \"Battlefield 1\"")]
        public async Task UnsubscribeAsync([Summary("The name of the game to unsubscribe from.")] [Remainder] string gameName)
        {
            if (gameName.IsEmpty())
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
                logger.LogWarning(ex, "Error removing a game subscription");
            }
            await ReplyAsync($"You are now unsubscribed from {gameName}");
        }
    }
}