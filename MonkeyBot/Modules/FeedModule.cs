using CodeHollow.FeedReader;
using Discord;
using Discord.Commands;
using dokas.FluentStrings;
using Microsoft.Extensions.Logging;
using MonkeyBot.Common;
using MonkeyBot.Preconditions;
using MonkeyBot.Services;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace MonkeyBot.Modules
{
    [Group("Feeds")]
    [Name("Feeds")]
    [MinPermissions(AccessLevel.ServerAdmin)]
    [RequireContext(ContextType.Guild)]
    public class FeedModule : ModuleBase
    {
        private readonly DbService dbService;
        private readonly IFeedService feedService;

        public FeedModule(DbService dbService, IFeedService backgroundService)
        {
            this.dbService = dbService;
            this.feedService = backgroundService;
        }

        [Command("Add")]
        [Remarks("Adds an atom or RSS feed to the list of listened feeds.")]
        public async Task AddFeedUrlAsync([Summary("The url to the feed (Atom/RSS)")] string url, [Summary("Optional: The name of the channel where the Feed updates should be posted. Defaults to current channel")] string channelName = "")
        {
            if (url.IsEmpty())
            {
                await ReplyAsync("Please enter a feed url!");
                return;
            }
            var allChannels = await Context.Guild.GetTextChannelsAsync();
            ITextChannel channel;
            if (!channelName.IsEmpty())
            {
                channel = allChannels.FirstOrDefault(x => x.Name.ToLower() == channelName.ToLower());
                if (channel == null)
                {
                    await ReplyAsync("The specified channel does not exist");
                    return;
                }
            }
            else
            {
                channel = Context.Channel as ITextChannel;
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
            await feedService.RunOnceSingleFeedAsync(Context.Guild.Id, Context.Channel.Id, feedUrl, true);
        }

        [Command("Remove")]
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

        [Command("RemoveAll")]
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
    }
}