using CodeHollow.FeedReader;
using CodeHollow.FeedReader.Feeds;
using Discord;
using Discord.WebSocket;
using dokas.FluentStrings;
using FluentScheduler;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyBot.Services
{
    public class FeedService : IFeedService
    {
        private const int updateIntervallMinutes = 30;

        private readonly DbService dbService;
        private readonly DiscordSocketClient discordClient;
        private readonly ILogger<FeedService> logger;
        private readonly ConcurrentDictionary<string, DateTime> lastFeedUpdate;

        public FeedService(DbService db, DiscordSocketClient client, ILogger<FeedService> logger)
        {
            this.dbService = db;
            this.discordClient = client;
            this.logger = logger;
            lastFeedUpdate = new ConcurrentDictionary<string, DateTime>();
        }

        public void Start()
        {
            JobManager.AddJob(async () => await GetAllFeedUpdatesAsync(), (x) => x.ToRunNow().AndEvery(updateIntervallMinutes).Minutes().DelayFor(10).Seconds());
        }

        public async Task RunOnceAllFeedsAsync(ulong guildId)
        {
            var guild = discordClient.GetGuild(guildId);
            if (guild != null)
                await GetGuildFeedUpdatesAsync(guild);
        }

        public async Task RunOnceSingleFeedAsync(ulong guildId, ulong channelId, string url, bool getLatest = false)
        {
            var guild = discordClient.GetGuild(guildId);
            var channel = guild?.GetTextChannel(channelId);
            if (guild != null && channel != null)
                await GetFeedUpdateAsync(channel, url, getLatest);
        }

        private async Task GetAllFeedUpdatesAsync()
        {
            foreach (var guild in discordClient?.Guilds)
            {
                await GetGuildFeedUpdatesAsync(guild);
            }
        }

        private async Task GetGuildFeedUpdatesAsync(SocketGuild guild)
        {
            List<string> feedUrls = null;
            SocketTextChannel channel = null;

            using (var uow = dbService?.UnitOfWork)
            {
                var cfg = await uow.GuildConfigs.GetAsync(guild.Id);
                if (cfg == null || !cfg.ListenToFeeds || cfg.FeedUrls == null || cfg.FeedUrls.Count < 1)
                    return;
                channel = guild.GetTextChannel(cfg.FeedChannelId);
                feedUrls = cfg.FeedUrls;
                if (channel == null)
                    return;
            }

            foreach (var feedUrl in feedUrls)
            {
                await GetFeedUpdateAsync(channel, feedUrl);
            }
        }

        private async Task GetFeedUpdateAsync(SocketTextChannel channel, string feedUrl, bool getLatest = false)
        {
            if (channel == null || feedUrl.IsEmpty())
                return;
            Feed feed;
            try
            {
                feed = await FeedReader.ReadAsync(feedUrl);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error getting feeds");
                return;
            }
            if (feed == null || feed.Items == null || feed.Items.Count < 1)
                return;
            var lastUpdate = DateTime.UtcNow;
            var guildFeedUrl = channel.Guild.Id + feedUrl;
            if (!lastFeedUpdate.TryGetValue(guildFeedUrl, out lastUpdate))
            {
                lastUpdate = DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(updateIntervallMinutes));
                lastFeedUpdate.TryAdd(guildFeedUrl, lastUpdate);
            }
            var allFeeds = feed?.Items?.Where(x => x.PublishingDate.HasValue);
            var updatedFeeds = allFeeds?.Where(x => x.PublishingDate.Value.ToUniversalTime() > lastUpdate).OrderBy(x => x.PublishingDate).ToList();
            if (updatedFeeds != null && updatedFeeds.Count == 0 && getLatest)
                updatedFeeds = allFeeds.Take(1).ToList();
            if (updatedFeeds != null && updatedFeeds.Count > 0)
            {
                var builder = new EmbedBuilder();
                builder.WithColor(new Color(21, 26, 35));
                if (!feed.ImageUrl.IsEmpty())
                    builder.WithImageUrl(feed.ImageUrl);
                string title = $"New update{(updatedFeeds.Count > 1 ? "s" : "")} for \"{ParseHtml(feed.Title) ?? feedUrl}".TruncateTo(255) + "\"";
                builder.WithTitle(title);
                DateTime latestUpdate = DateTime.MinValue;
                foreach (var feedItem in updatedFeeds)
                {
                    if (feedItem.PublishingDate.HasValue && feedItem.PublishingDate.Value > latestUpdate)
                        latestUpdate = feedItem.PublishingDate.Value;
                    string fieldName = feedItem.PublishingDate.HasValue ? feedItem.PublishingDate.Value.ToLocalTime().ToString() : feedItem.PublishingDateString;
                    string author = feedItem.Author;
                    if (author.IsEmpty())
                    {
                        if (feed.Type == FeedType.Rss_1_0)
                            author = (feedItem.SpecificItem as Rss10FeedItem)?.DC?.Creator;
                        else if (feed.Type == FeedType.Rss_2_0)
                            author = (feedItem.SpecificItem as Rss20FeedItem)?.DC?.Creator;
                    }
                    author = !author.IsEmpty().OrWhiteSpace() ? $"{author.Trim()}: " : string.Empty;
                    string maskedLink = $"[{author}{ParseHtml(feedItem.Title).Trim()}]({feedItem.Link})";
                    string description = ParseHtml(feedItem.Description).Trim().TruncateTo(250).WithEllipsis();
                    if (description.IsEmpty().OrWhiteSpace())
                        description = "[...]";
                    string fieldContent = $"{maskedLink}{Environment.NewLine}*{description}".TruncateTo(1023) + "*"; // Embed field value must be <= 1024 characters
                    builder.AddInlineField(fieldName, fieldContent);
                }
                await channel?.SendMessageAsync("", false, builder.Build());
                if (latestUpdate > DateTime.MinValue)
                {
                    if (lastFeedUpdate.TryGetValue(guildFeedUrl, out var oldValue))
                        lastFeedUpdate.TryUpdate(guildFeedUrl, latestUpdate, oldValue);
                    else
                        lastFeedUpdate.TryAdd(guildFeedUrl, latestUpdate);
                }
            }
        }

        private static string ParseHtml(string html)
        {
            if (html.IsEmpty())
                return html;
            html = System.Web.HttpUtility.HtmlDecode(html);
            var htmlDoc = new HtmlDocument();

            htmlDoc.LoadHtml(html);
            var sb = new StringBuilder();

            var textNodes = htmlDoc?.DocumentNode?.SelectNodes("//text()");
            var iframes = htmlDoc?.DocumentNode?.SelectNodes("//iframe[@src]");
            if (textNodes != null)
            {
                foreach (HtmlNode node in textNodes)
                {
                    if (!node.InnerText.IsEmpty().OrWhiteSpace())
                        sb.Append(node.InnerText.Trim());
                }
            }
            if (iframes != null)
            {
                foreach (HtmlNode node in iframes)
                {
                    sb.Append(node.Attributes["src"].Value);
                }
            }
            var result = sb.ToString();
            if (!result.IsEmpty())
                return result;
            return html;
        }
    }
}