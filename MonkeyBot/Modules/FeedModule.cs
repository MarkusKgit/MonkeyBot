using CodeHollow.FeedReader;
using Discord;
using Discord.Commands;
using MonkeyBot.Common;
using MonkeyBot.Preconditions;
using MonkeyBot.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyBot.Modules
{
    [Group("Feeds")]
    [Name("Feeds")]
    [MinPermissions(AccessLevel.ServerAdmin)]
    [RequireContext(ContextType.Guild)]
    [RequireBotPermission(GuildPermission.EmbedLinks)]
    public class FeedModule : MonkeyModuleBase
    {
        private readonly IFeedService feedService;

        public FeedModule(IFeedService feedService)
        {
            this.feedService = feedService;
        }

        [Command("Add")]
        [Remarks("Adds an atom or RSS feed to the list of listened feeds.")]
        [Example("!Feeds add https://blogs.msdn.microsoft.com/dotnet/feed/")]
        public async Task AddFeedUrlAsync([Summary("The name/title of the feed")] string name, [Summary("The url to the feed (Atom/RSS)")] string url, [Summary("Optional: The name of the channel where the Feed updates should be posted. Defaults to current channel")] string channelName = "")
        {
            if (name.IsEmpty())
            {
                _ = await ReplyAsync("Please enter a name for the feed!").ConfigureAwait(false);
                return;
            }
            if (url.IsEmpty())
            {
                _ = await ReplyAsync("Please enter a feed url!").ConfigureAwait(false);
                return;
            }
            ITextChannel channel = await GetTextChannelInGuildAsync(channelName, true).ConfigureAwait(false);
            if (channel == null)
            {
                _ = await ReplyAsync("The specified channel was not found").ConfigureAwait(false);
                return;
            }
            IEnumerable<HtmlFeedLink> urls = await FeedReader.GetFeedUrlsFromUrlAsync(url).ConfigureAwait(false);
            string feedUrl;
            if (!urls.Any()) // no url - probably the url is already the right feed url
            {
                feedUrl = url;
            }
            else if (urls.Count() == 1)
            {
                feedUrl = urls.First().Url;
            }
            else
            {
                _ = await ReplyAsync($"Multiple feeds were found at this url. Please be more specific:{Environment.NewLine}{string.Join(Environment.NewLine, urls)}").ConfigureAwait(false);
                return;
            }
            List<(string name, string feedUrl, ulong feedChannelId)> currentFeeds = await feedService.GetFeedsForGuildAsync(Context.Guild.Id, channel.Id).ConfigureAwait(false);
            if (currentFeeds.Any(x => x.feedUrl == feedUrl || x.name == name))
            {
                _ = await ReplyAsync("The specified feed is already in the list!").ConfigureAwait(false);
                return;
            }
            await feedService.AddFeedAsync(name, feedUrl, Context.Guild.Id, channel.Id).ConfigureAwait(false);
            _ = await ReplyAsync("Feed added").ConfigureAwait(false);
        }

        [Command("Remove")]
        [Remarks("Removes the specified feed from the list of feeds.")]
        [Example("!Feeds remove https://blogs.msdn.microsoft.com/dotnet/feed/")]
        public async Task RemoveFeedUrlAsync([Summary("The name or the url of the feed")] string nameOrUrl, [Summary("Optional: The name of the channel where the Feed url should be removed. Defaults to current channel")] string channelName = "")
        {
            if (nameOrUrl.IsEmpty())
            {
                _ = await ReplyAsync("Please enter a feed url").ConfigureAwait(false);
                return;
            }
            ITextChannel channel = await GetTextChannelInGuildAsync(channelName, true).ConfigureAwait(false);
            if (channel == null)
            {
                _ = await ReplyAsync("The specified channel was not found").ConfigureAwait(false);
                return;
            }
            List<(string name, string feedUrl, ulong feedChannelId)> currentFeeds = await feedService.GetFeedsForGuildAsync(Context.Guild.Id, channel?.Id).ConfigureAwait(false);
            if (!currentFeeds.Any(x => x.feedUrl == nameOrUrl || x.name == nameOrUrl))
            {
                _ = await ReplyAsync("The specified feed is not in the list!").ConfigureAwait(false);
                return;
            }

            await feedService.RemoveFeedAsync(nameOrUrl, Context.Guild.Id, channel.Id).ConfigureAwait(false);
            _ = await ReplyAsync("Feed removed").ConfigureAwait(false);
        }

        [Command("List")]
        [Remarks("List all current feed urls")]
        public async Task ListFeedUrlsAsync([Summary("Optional: The name of the channel where the Feed urls should be listed for. Defaults to all channels")] string channelName = "")
        {
            ITextChannel channel = await GetTextChannelInGuildAsync(channelName, false).ConfigureAwait(false);
            List<(string name, string feedUrl, ulong feedChannelId)> feeds = await feedService.GetFeedsForGuildAsync(Context.Guild.Id, channel?.Id).ConfigureAwait(false);
            if (feeds == null || feeds.Count < 1)
            {
                _ = await ReplyAsync("No feeds have been added yet.").ConfigureAwait(false);
            }
            else
            {
                if (channel == null)
                {
                    var sb = new StringBuilder();
                    foreach ((string feedName, string feedUrl, ulong feedChannelId) in feeds)
                    {
                        ITextChannel feedChannel = await Context.Guild.GetTextChannelAsync(feedChannelId).ConfigureAwait(false);
                        _ = sb.AppendLine($"{feedChannel.Mention}: {feedName} ({feedUrl})");
                    }
                    _ = await ReplyAsync($"The following feeds are listed in all channels:{Environment.NewLine}{sb.ToString()}").ConfigureAwait(false);
                }
                else
                {
                    string allUrls = string.Join(Environment.NewLine, feeds.Select(x => x.feedUrl));
                    _ = await ReplyAsync($"The following feeds are listed in {channel.Mention}:{Environment.NewLine}{string.Join(Environment.NewLine, feeds.Select(x => x.feedUrl))}").ConfigureAwait(false);
                }
            }
        }

        [Command("RemoveAll")]
        [Remarks("Removes all feed urls")]
        public async Task RemoveFeedUrlsAsync([Summary("Optional: The name of the channel where the Feed urls should be removed. Defaults to all channels")] string channelName = "")
        {
            ITextChannel channel = await GetTextChannelInGuildAsync(channelName, false).ConfigureAwait(false);
            await feedService.RemoveAllFeedsAsync(Context.Guild.Id, channel?.Id).ConfigureAwait(false);
        }
    }
}