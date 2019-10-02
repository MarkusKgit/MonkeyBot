using CodeHollow.FeedReader;
using CodeHollow.FeedReader.Feeds;
using Discord;
using Discord.WebSocket;
using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MonkeyBot.Common;
using MonkeyBot.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyBot.Services
{
    public class FeedService : IFeedService
    {
        private const int updateIntervallMinutes = 30;

        private readonly MonkeyDBContext dbContext;
        private readonly DiscordSocketClient discordClient;
        private readonly ISchedulingService schedulingService;
        private readonly ILogger<FeedService> logger;

        public FeedService(MonkeyDBContext dbContext, DiscordSocketClient discordClient, ISchedulingService schedulingService, ILogger<FeedService> logger)
        {
            this.dbContext = dbContext;
            this.discordClient = discordClient;
            this.schedulingService = schedulingService;
            this.logger = logger;
        }

        public void Start()
        {
            schedulingService.ScheduleJobRecurring("feeds", updateIntervallMinutes * 60, async () => await GetAllFeedUpdatesAsync().ConfigureAwait(false), 10);
        }

        public async Task AddFeedAsync(string name, string url, ulong guildID, ulong channelID)
        {
            var feed = new Models.Feed
            {
                Name = name,
                URL = url,
                GuildID = guildID,
                ChannelID = channelID
            };
            dbContext.Feeds.Add(feed);
            await dbContext.SaveChangesAsync().ConfigureAwait(false);
            await GetFeedUpdateAsync(feed, true).ConfigureAwait(false);
        }

        public async Task RemoveFeedAsync(string nameOrUrl, ulong guildID, ulong channelID)
        {
            var feed = await dbContext.Feeds.SingleOrDefaultAsync(f => f.Name == nameOrUrl || f.URL == nameOrUrl && f.GuildID == guildID && f.ChannelID == channelID).ConfigureAwait(false);
            if (feed != null)
            {
                dbContext.Feeds.Remove(feed);
                await dbContext.SaveChangesAsync().ConfigureAwait(false);
            }
        }

        public async Task RemoveAllFeedsAsync(ulong guildID, ulong? channelID)
        {
            var allFeeds = await GetAllFeedsInternalAsync(guildID, channelID).ConfigureAwait(false);
            if (allFeeds == null)
                return;
            dbContext.Feeds.RemoveRange(allFeeds);
            await dbContext.SaveChangesAsync().ConfigureAwait(false);
        }

        public async Task<List<(string name, string feedUrl, ulong feedChannelId)>> GetFeedsForGuildAsync(ulong guildId, ulong? channelId = null)
        {
            var allFeeds = await GetAllFeedsInternalAsync(guildId, channelId).ConfigureAwait(false);
            return allFeeds?.Select(x => (x.Name, x.URL, x.ChannelID)).ToList();
        }

        private Task<List<Models.Feed>> GetAllFeedsInternalAsync(ulong guildID, ulong? channelID = null)
        {
            if (channelID.HasValue)
                return dbContext.Feeds.Where(x => x.GuildID == guildID && x.ChannelID == channelID.Value).ToListAsync();
            else
                return dbContext.Feeds.Where(x => x.GuildID == guildID).ToListAsync();
        }

        private async Task GetAllFeedUpdatesAsync()
        {
            foreach (var guild in discordClient?.Guilds)
            {
                await GetGuildFeedUpdatesAsync(guild).ConfigureAwait(false);
            }
        }

        private async Task GetGuildFeedUpdatesAsync(SocketGuild guild)
        {
            var feeds = await GetAllFeedsInternalAsync(guild.Id).ConfigureAwait(false);
            foreach (var feed in feeds)
            {
                await GetFeedUpdateAsync(feed).ConfigureAwait(false);
            }
        }

        private async Task GetFeedUpdateAsync(Models.Feed guildFeed, bool getLatest = false)
        {
            var guild = discordClient.GetGuild(guildFeed.GuildID);
            var channel = guild?.GetTextChannel(guildFeed.ChannelID);
            if (guild == null || channel == null || guildFeed.URL.IsEmpty())
                return;
            Feed feed;
            try
            {
                feed = await FeedReader.ReadAsync(guildFeed.URL).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, $"Error getting feeds for {guildFeed.URL} in {guild.Name}:{channel.Name}");
                return;
            }
            if (feed == null || feed.Items == null || feed.Items.Count < 1)
                return;
            var lastUpdateUTC = DateTime.UtcNow;
            if (guildFeed.LastUpdate.HasValue)
                lastUpdateUTC = guildFeed.LastUpdate.Value;
            else
                lastUpdateUTC = DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(updateIntervallMinutes));
            var allFeeds = feed?.Items?.Where(x => x.PublishingDate.HasValue);
            var updatedFeeds = allFeeds?.Where(x => x.PublishingDate.Value.ToUniversalTime() > lastUpdateUTC).OrderBy(x => x.PublishingDate).ToList();
            if (updatedFeeds != null && updatedFeeds.Count == 0 && getLatest)
                updatedFeeds = allFeeds.Take(1).ToList();
            if (updatedFeeds != null && updatedFeeds.Count > 0)
            {
                var builder = new EmbedBuilder();
                builder.WithColor(new Color(21, 26, 35));
                if (!feed.ImageUrl.IsEmpty())
                    builder.WithImageUrl(feed.ImageUrl);
                var feedTitle = ParseHtml(feed.Title);
                if (feedTitle.IsEmptyOrWhiteSpace())
                    feedTitle = guildFeed.Name;
                var title = $"New update{(updatedFeeds.Count > 1 ? "s" : "")} for \"{feedTitle}".TruncateTo(255, "") + "\"";
                builder.WithTitle(title);
                var latestUpdateUTC = DateTime.MinValue;
                foreach (var feedItem in updatedFeeds)
                {
                    if (feedItem.PublishingDate.HasValue && feedItem.PublishingDate.Value.ToUniversalTime() > latestUpdateUTC)
                        latestUpdateUTC = feedItem.PublishingDate.Value.ToUniversalTime();
                    var fieldName = feedItem.PublishingDate.HasValue ? feedItem.PublishingDate.Value.ToLocalTime().ToString() : DateTime.Now.ToString();
                    var author = feedItem.Author;
                    if (author.IsEmpty())
                    {
                        if (feed.Type == FeedType.Rss_1_0)
                            author = (feedItem.SpecificItem as Rss10FeedItem)?.DC?.Creator;
                        else if (feed.Type == FeedType.Rss_2_0)
                            author = (feedItem.SpecificItem as Rss20FeedItem)?.DC?.Creator;
                    }
                    author = !author.IsEmptyOrWhiteSpace() ? $"{author.Trim()}: " : string.Empty;
                    var maskedLink = $"[{author}{ParseHtml(feedItem.Title).Trim()}]({feedItem.Link})";
                    var content = ParseHtml(feedItem.Description).Trim();
                    if (content.IsEmptyOrWhiteSpace())
                        content = ParseHtml(feedItem.Content).Trim();
                    if (content.IsEmptyOrWhiteSpace())
                        content = "[...]";
                    else
                        content = content.TruncateTo(250, "");
                    var fieldContent = $"{maskedLink}{Environment.NewLine}*{content}".TruncateTo(1023, "") + "*"; // Embed field value must be <= 1024 characters
                    builder.AddField(fieldName, fieldContent, true);
                }
                await (channel?.SendMessageAsync("", false, builder.Build())).ConfigureAwait(false);
                if (latestUpdateUTC > DateTime.MinValue)
                {
                    guildFeed.LastUpdate = latestUpdateUTC;
                    dbContext.Feeds.Update(guildFeed);
                    await dbContext.SaveChangesAsync().ConfigureAwait(false);
                }
            }
        }

        private string ParseHtml(string html)
        {
            if (html.IsEmptyOrWhiteSpace())
                return string.Empty;

            var htmlDoc = new HtmlDocument();

            try
            {
                html = MonkeyHelpers.CleanHtmlString(html);
                htmlDoc.LoadHtml(html);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error parsing html");
                return html;
            }

            var sb = new StringBuilder();

            var textNodes = htmlDoc?.DocumentNode?.SelectNodes("//text()");
            var iframes = htmlDoc?.DocumentNode?.SelectNodes("//iframe[@src]");
            if (textNodes != null)
            {
                foreach (var node in textNodes)
                {
                    if (!node.InnerText.IsEmptyOrWhiteSpace())
                        sb.Append(node.InnerText.Trim() + "|");
                }
            }
            if (iframes != null)
            {
                foreach (var node in iframes)
                {
                    if (node.HasAttributes &&
                        node.Attributes.Contains("src") &&
                        !node.Attributes["src"].Value.IsEmptyOrWhiteSpace())
                    {
                        sb.Append(node.Attributes["src"].Value);
                    }
                }
            }
            var result = sb.ToString();
            if (!result.IsEmpty())
                return result;
            return html;
        }
    }
}