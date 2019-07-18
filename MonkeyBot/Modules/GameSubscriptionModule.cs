using Discord;
using Discord.Commands;
using dokas.FluentStrings;
using Microsoft.Extensions.Logging;
using MonkeyBot.Common;
using MonkeyBot.Preconditions;
using MonkeyBot.Services;
using System;
using System.Linq;
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
                await ReplyAsync("You need to specify a game you wish to subscribe to!").ConfigureAwait(false);
                return;
            }
            try
            {
                // Add the Subscription to the Service to activate it
                await gameSubscriptionService.AddSubscriptionAsync(gameName, Context.Guild.Id, Context.User.Id).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await ReplyAsync($"There was an error while adding the subscription:{Environment.NewLine}{ex.Message}").ConfigureAwait(false);
                logger.LogWarning(ex, "Error adding a game subscription");
            }
            await ReplyAsync($"You are now subscribed to {gameName}").ConfigureAwait(false);
        }

        [Command("Unsubscribe")]
        [Remarks("Unsubscribes to the specified game")]
        [Example("!Unsubscribe \"Battlefield 1\"")]
        public async Task UnsubscribeAsync([Summary("The name of the game to unsubscribe from.")] [Remainder] string gameName)
        {
            if (gameName.IsEmpty())
            {
                await ReplyAsync("You need to specify a game you wish to unsubscribe from!").ConfigureAwait(false);
                return;
            }
            try
            {
                // Remove the subscription from the Service
                await gameSubscriptionService.RemoveSubscriptionAsync(gameName, Context.Guild.Id, Context.User.Id).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await ReplyAsync($"There was an error while trying to remove the subscription:{Environment.NewLine}{ex.Message}").ConfigureAwait(false);
                logger.LogWarning(ex, "Error removing a game subscription");
                return;
            }
            await ReplyAsync($"You are now unsubscribed from {gameName}").ConfigureAwait(false);
        }

        [Command("Subscriptions")]
        [Remarks("Lists all your game subscriptions")]
        [Example("!Subscriptions")]
        public async Task ListAllAsync()
        {
            var subscriptions = await gameSubscriptionService.GetSubscriptionsForUser(Context.User.Id).ConfigureAwait(false);
            if (subscriptions == null || subscriptions.Count < 1)
            {
                await ReplyAsync("You are not subscribed to any game").ConfigureAwait(false);
            }
            else
            {
                string[] sSubscriptions = await Task.WhenAll(subscriptions.Select(async s => $"{s.GameName} in {(await Context.Client.GetGuildAsync(s.GuildID).ConfigureAwait(false)).Name}")).ConfigureAwait(false);
                await Context.User.SendMessageAsync($"You are subscribed to the following games {string.Join(", ", sSubscriptions)}").ConfigureAwait(false);
                await ReplyAndDeleteAsync("I have sent you a private message").ConfigureAwait(false);
            }
        }

    }
}