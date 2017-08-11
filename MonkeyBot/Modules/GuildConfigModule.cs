using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using MonkeyBot.Common;
using MonkeyBot.Preconditions;
using MonkeyBot.Services;
using System;
using System.Threading.Tasks;

namespace MonkeyBot.Modules
{
    [MinPermissions(AccessLevel.ServerAdmin)]
    [Name("Guild Configuration")]
    [RequireContext(ContextType.Guild)]
    public class GuildConfigModule : ModuleBase
    {
        private DbService db;

        public GuildConfigModule(IServiceProvider provider)
        {
            db = provider.GetService<DbService>();
        }

        [Command("SetWelcomeMsg")]
        [Remarks("Sets the welcome message for new users. Can make use of %user% and %server%")]
        public async Task SetWelcomeMessageAsync([Summary("The welcome message")][Remainder] string welcomeMsg)
        {
            welcomeMsg = welcomeMsg.Trim('\"');
            if (string.IsNullOrEmpty(welcomeMsg))
            {
                await ReplyAsync("Please provide a welcome message");
                return;
            }

            using (var uow = db.UnitOfWork)
            {
                var config = await uow.GuildConfigs.GetAsync(Context.Guild.Id);
                if (config == null)
                    config = new GuildConfig(Context.Guild.Id);
                config.WelcomeMessageText = welcomeMsg;
                await uow.GuildConfigs.AddOrUpdateAsync(config);
                await uow.CompleteAsync();
            }
        }

        [Command("AddRule")]
        [Remarks("Adds a rule to the server.")]
        public async Task AddRuleAsync([Summary("The rule to add")][Remainder] string rule)
        {
            if (string.IsNullOrEmpty(rule))
            {
                await ReplyAsync("Please enter a rule");
                return;
            }
            using (var uow = db.UnitOfWork)
            {
                var config = await uow.GuildConfigs.GetAsync(Context.Guild.Id);
                if (config == null)
                    config = new GuildConfig(Context.Guild.Id);
                config.Rules.Add(rule);
                await uow.GuildConfigs.AddOrUpdateAsync(config);
                await uow.CompleteAsync();
            }
        }

        [Command("RemoveRules")]
        [Remarks("Removes the rules from a server.")]
        public async Task RemoveRulesAsync()
        {
            using (var uow = db.UnitOfWork)
            {
                var config = await uow.GuildConfigs.GetAsync(Context.Guild.Id);
                if (config != null)
                {
                    config.Rules.Clear();
                    await uow.GuildConfigs.AddOrUpdateAsync(config);
                    await uow.CompleteAsync();
                }
            }
        }

        [Command("AddFeedUrl")]
        [Remarks("Adds a rule to the server.")]
        public async Task AddFeedUrlAsync([Summary("The url to the rss feed")][Remainder] string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                await ReplyAsync("Please enter a feed url");
                return;
            }
            using (var uow = db.UnitOfWork)
            {
                var config = await uow.GuildConfigs.GetAsync(Context.Guild.Id);
                if (config == null)
                    config = new GuildConfig(Context.Guild.Id);
                config.FeedUrls.Add(url);
                await uow.GuildConfigs.AddOrUpdateAsync(config);
                await uow.CompleteAsync();
            }
        }

        [Command("RemoveFeedUrls")]
        [Remarks("Removes all feed urls")]
        public async Task RemoveFeedUrlsAsync()
        {
            using (var uow = db.UnitOfWork)
            {
                var config = await uow.GuildConfigs.GetAsync(Context.Guild.Id);
                if (config != null)
                {
                    config.FeedUrls.Clear();
                    await uow.GuildConfigs.AddOrUpdateAsync(config);
                    await uow.CompleteAsync();
                }
            }
        }

        [Command("EnableFeeds")]
        [Remarks("Enables the feed listener")]
        public async Task EnableFeedsAsync()
        {
            await ToggleFeedsInternal(true);
        }

        [Command("DisableFeeds")]
        [Remarks("Disables the feed listener")]
        public async Task DisableFeedsAsync()
        {
            await ToggleFeedsInternal(false);
        }

        private async Task ToggleFeedsInternal(bool enable)
        {
            using (var uow = db.UnitOfWork)
            {
                var config = await uow.GuildConfigs.GetAsync(Context.Guild.Id);
                if (config == null)
                    config = new GuildConfig(Context.Guild.Id);
                config.ListenToFeeds = enable;
                await uow.GuildConfigs.AddOrUpdateAsync(config);
                await uow.CompleteAsync();
            }
            await ReplyAsync($"Feeds have been {(enable ? "enabled" : "disabled.")}");
        }
    }
}