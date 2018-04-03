using CodeHollow.FeedReader;
using Discord;
using Discord.Commands;
using dokas.FluentStrings;
using MonkeyBot.Common;
using MonkeyBot.Preconditions;
using MonkeyBot.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MonkeyBot.Modules
{
    [MinPermissions(AccessLevel.ServerAdmin)]
    [Name("Guild Configuration")]
    [RequireContext(ContextType.Guild)]
    public class GuildConfigModule : ModuleBase
    {
        private readonly DbService dbService;
        private readonly IBackgroundService backgroundService;

        public GuildConfigModule(DbService db, IBackgroundService backgroundService)
        {
            this.dbService = db;
            this.backgroundService = backgroundService;
        }

        #region WelcomeMessage

        [Command("SetWelcomeMessage")]
        [Remarks("Sets the welcome message for new users. Can make use of %user% and %server%")]
        [Example("!SetWelcomeMessage \"Hello %user%, welcome to %server$\"")]
        public async Task SetWelcomeMessageAsync([Summary("The welcome message")][Remainder] string welcomeMsg)
        {
            welcomeMsg = welcomeMsg.Trim('\"');
            if (welcomeMsg.IsEmpty())
            {
                await ReplyAsync("Please provide a welcome message");
                return;
            }

            using (var uow = dbService.UnitOfWork)
            {
                var config = await uow.GuildConfigs.GetAsync(Context.Guild.Id);
                if (config == null)
                    config = new GuildConfig(Context.Guild.Id);
                config.WelcomeMessageText = welcomeMsg;
                await uow.GuildConfigs.AddOrUpdateAsync(config);
                await uow.CompleteAsync();
            }
        }

        #endregion WelcomeMessage

        #region Rules

        [Command("AddRule")]
        [Remarks("Adds a rule to the server.")]
        [Example("!AddRule \"You shall not pass!\"")]
        public async Task AddRuleAsync([Summary("The rule to add")][Remainder] string rule)
        {
            if (rule.IsEmpty())
            {
                await ReplyAsync("Please enter a rule");
                return;
            }
            using (var uow = dbService.UnitOfWork)
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
        [Remarks("Removes all rules from a server.")]
        public async Task RemoveRulesAsync()
        {
            using (var uow = dbService.UnitOfWork)
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

        #endregion Rules

        #region Feeds

        [Command("AddFeedUrl")]
        [Remarks("Adds an atom or RSS feed to the list of listened feeds.")]
        public async Task AddFeedUrlAsync([Summary("The url to the feed (Atom/RSS)")][Remainder] string url)
        {
            if (url.IsEmpty())
            {
                await ReplyAsync("Please enter a feed url");
                return;
            }
            var urls = await FeedReader.GetFeedUrlsFromUrlAsync(url);
            string feedUrl;
            if (urls.Count() < 1) // no url - probably the url is already the right feed url
                feedUrl = url;
            else if (urls.Count() == 1)
                feedUrl = urls.First().Url;
            else
            {
                await ReplyAsync($"Multiple feeds were found at this url. Please be more specific:{Environment.NewLine}{string.Join(Environment.NewLine, urls)}");
                return;
            }
            using (var uow = dbService.UnitOfWork)
            {
                var config = await uow.GuildConfigs.GetAsync(Context.Guild.Id);
                if (config == null)
                    config = new GuildConfig(Context.Guild.Id);
                if (config.FeedUrls.Contains(feedUrl))
                {
                    await ReplyAsync("The specified feed is already in the list!");
                    return;
                }
                config.FeedUrls.Add(feedUrl);
                await uow.GuildConfigs.AddOrUpdateAsync(config);
                await uow.CompleteAsync();
                await ReplyAsync("Feed added");
            }
            await backgroundService.RunOnceSingleFeedAsync(Context.Guild.Id, Context.Channel.Id, feedUrl, true);
        }

        [Command("RemoveFeedUrl")]
        [Remarks("Removes the specified feed from the list of feeds.")]
        public async Task RemoveFeedUrlAsync([Summary("The url of the feed")][Remainder] string url)
        {
            if (url.IsEmpty())
            {
                await ReplyAsync("Please enter a feed url");
                return;
            }
            using (var uow = dbService.UnitOfWork)
            {
                var config = await uow.GuildConfigs.GetAsync(Context.Guild.Id);
                if (config == null)
                    config = new GuildConfig(Context.Guild.Id);
                if (!config.FeedUrls.Contains(url))
                {
                    await ReplyAsync("The specified feed is not in the list!");
                    return;
                }
                config.FeedUrls.Remove(url);
                await uow.GuildConfigs.AddOrUpdateAsync(config);
                await uow.CompleteAsync();
                await ReplyAsync("Feed removed");
            }
        }

        [Command("RemoveFeedUrls")]
        [Remarks("Removes all feed urls")]
        public async Task RemoveFeedUrlsAsync()
        {
            using (var uow = dbService.UnitOfWork)
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
        [Remarks("Enables the feed listener in the specified channel")]
        [Example("!EnableFeeds general")]
        public async Task EnableFeedsAsync([Summary("The channel where the feed updates should be broadcasted")]string channelName = "")
        {
            IGuildChannel channel;
            if (channelName == "")
                channel = Context.Channel as IGuildChannel;
            else
                channel = (await Context.Guild?.GetChannelsAsync())?.FirstOrDefault(x => x.Name == channelName);
            if (channel == null)
            {
                await ReplyAsync("The specified channel does not exist");
                return;
            }
            await ToggleFeedsInternalAsync(true, channel.Id);
            await backgroundService?.RunOnceAllFeedsAsync(Context.Guild.Id);
        }

        [Command("DisableFeeds")]
        [Remarks("Disables the feed listener")]
        public async Task DisableFeedsAsync()
        {
            await ToggleFeedsInternalAsync(false);
        }

        private async Task ToggleFeedsInternalAsync(bool enable, ulong channelID = 0)
        {
            using (var uow = dbService.UnitOfWork)
            {
                var config = await uow.GuildConfigs.GetAsync(Context.Guild.Id);
                if (config == null)
                    config = new GuildConfig(Context.Guild.Id);
                config.ListenToFeeds = enable;
                if (enable)
                    config.FeedChannelId = channelID;
                await uow.GuildConfigs.AddOrUpdateAsync(config);
                await uow.CompleteAsync();
            }
            await ReplyAsync($"Feeds have been {(enable ? "enabled" : "disabled.")}");
        }

        #endregion Feeds
    }
}