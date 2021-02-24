using CodeHollow.FeedReader;
using CodeHollow.FeedReader.Feeds;
using DSharpPlus;
using DSharpPlus.Entities;
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
        private readonly DiscordClient discordClient;
        private readonly ISchedulingService schedulingService;
        private readonly ILogger<FeedService> logger;

        public FeedService(MonkeyDBContext dbContext, DiscordClient discordClient, ISchedulingService schedulingService, ILogger<FeedService> logger)
        {
            this.dbContext = dbContext;
            this.discordClient = discordClient;
            this.schedulingService = schedulingService;
            this.logger = logger;
        }

        public void Start()
            => schedulingService.ScheduleJobRecurring("feeds", updateIntervallMinutes * 60, async () => await GetAllFeedUpdatesAsync(), 10);

        public async Task AddFeedAsync(string name, string url, ulong guildID, ulong channelID)
        {
            var feed = new Models.Feed
            {
                Name = name,
                URL = url,
                GuildID = guildID,
                ChannelID = channelID
            };
            _ = dbContext.Feeds.Add(feed);
            _ = await dbContext.SaveChangesAsync();
            await GetFeedUpdateAsync(feed, true);
        }

        public async Task RemoveFeedAsync(string nameOrUrl, ulong guildID)
        {
            Models.Feed feed = await dbContext.Feeds.AsQueryable().SingleOrDefaultAsync(f => f.Name == nameOrUrl || (f.URL == nameOrUrl && f.GuildID == guildID));
            if (feed != null)
            {
                _ = dbContext.Feeds.Remove(feed);
                _ = await dbContext.SaveChangesAsync();
            }
        }

        public async Task RemoveAllFeedsAsync(ulong guildID, ulong? channelID)
        {
            List<Models.Feed> allFeeds = await GetAllFeedsInternalAsync(guildID, channelID);
            if (allFeeds == null)
            {
                return;
            }
            dbContext.Feeds.RemoveRange(allFeeds);
            _ = await dbContext.SaveChangesAsync();
        }

        public async Task<List<GuildFeed>> GetFeedsForGuildAsync(ulong guildId, ulong? channelId = null)
        {
            List<Models.Feed> allFeeds = await GetAllFeedsInternalAsync(guildId, channelId);
            return allFeeds?.Select(x => new GuildFeed(x.Name, x.URL, x.ChannelID)).ToList();
        }

        private Task<List<Models.Feed>> GetAllFeedsInternalAsync(ulong guildID, ulong? channelID = null)
        {
            return channelID.HasValue
                ? dbContext.Feeds.AsQueryable().Where(x => x.GuildID == guildID && x.ChannelID == channelID.Value).ToListAsync()
                : dbContext.Feeds.AsQueryable().Where(x => x.GuildID == guildID).ToListAsync();
        }

        private async Task GetAllFeedUpdatesAsync()
        {
            foreach (DiscordGuild guild in discordClient?.Guilds.Values)
            {
                await GetGuildFeedUpdatesAsync(guild);
            }
        }

        private async Task GetGuildFeedUpdatesAsync(DiscordGuild guild)
        {
            List<Models.Feed> feeds = await GetAllFeedsInternalAsync(guild.Id);
            foreach (Models.Feed feed in feeds)
            {
                await GetFeedUpdateAsync(feed);
            }
        }

        private async Task GetFeedUpdateAsync(Models.Feed guildFeed, bool getLatest = false)
        {            
            if (!discordClient.Guilds.TryGetValue(guildFeed.GuildID, out DiscordGuild guild))
            {
                return;
            }
            DiscordChannel channel = guild?.GetChannel(guildFeed.ChannelID);
            if (guild == null || channel == null || guildFeed.URL.IsEmpty())
            {
                return;
            }
            Feed feed;
            try
            {
                feed = await FeedReader.ReadAsync(guildFeed.URL);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, $"Error getting feeds for {guildFeed.URL} in {guild.Name}:{channel.Name}");
                return;
            }
            if (feed == null || feed.Items == null || feed.Items.Count < 1)
            {
                return;
            }

            DateTime lastUpdateUTC = guildFeed.LastUpdate ?? DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(updateIntervallMinutes));
            IEnumerable<FeedItem> allFeeds = feed?.Items?.Where(x => x.PublishingDate.HasValue);
            List<FeedItem> updatedFeeds = allFeeds?.Where(x => x.PublishingDate.Value.ToUniversalTime() > lastUpdateUTC)
                                                   .OrderBy(x => x.PublishingDate)
                                                   .ToList();
            if (updatedFeeds != null && updatedFeeds.Count == 0 && getLatest)
            {
                updatedFeeds = allFeeds.Take(1).ToList();
            }
            if (updatedFeeds != null && updatedFeeds.Count > 0)
            {
                var builder = new DiscordEmbedBuilder();
                _ = builder.WithColor(new DiscordColor(21, 26, 35));
                if (!feed.ImageUrl.IsEmpty())
                {
                    _ = builder.WithImageUrl(new Uri(feed.ImageUrl));
                }
                _ = builder.WithTitle($"New update{(updatedFeeds.Count > 1 ? "s" : "")} for \"{guildFeed.Name}".TruncateTo(255, "") + "\"");
                DateTime latestUpdateUTC = DateTime.MinValue;
                foreach (FeedItem feedItem in updatedFeeds)
                {
                    if (feedItem.PublishingDate.HasValue && feedItem.PublishingDate.Value.ToUniversalTime() > latestUpdateUTC)
                    {
                        latestUpdateUTC = feedItem.PublishingDate.Value.ToUniversalTime();
                    }
                    string fieldName = feedItem.PublishingDate.HasValue
                        ? feedItem.PublishingDate.Value.ToLocalTime().ToString()
                        : DateTime.Now.ToString();
                    string author = feedItem.Author;
                    if (author.IsEmpty())
                    {
                        if (feed.Type == FeedType.Rss_1_0)
                        {
                            author = (feedItem.SpecificItem as Rss10FeedItem)?.DC?.Creator;
                        }
                        else if (feed.Type == FeedType.Rss_2_0)
                        {
                            author = (feedItem.SpecificItem as Rss20FeedItem)?.DC?.Creator;
                        }
                    }
                    author = !author.IsEmptyOrWhiteSpace() ? $"{author.Trim()}: " : string.Empty;
                    string maskedLink = $"[{author}{ParseHtml(feedItem.Title).Trim()}]({feedItem.Link})";
                    string content = ParseHtml(feedItem.Description).Trim();
                    if (content.IsEmptyOrWhiteSpace())
                    {
                        content = ParseHtml(feedItem.Content).Trim();
                    }
                    content = content.IsEmptyOrWhiteSpace() ? "[...]" : content.TruncateTo(250, "");
                    string fieldContent = $"{maskedLink}{Environment.NewLine}*{content}".TruncateTo(1023, "") + "*"; // Embed field value must be <= 1024 characters
                    _ = builder.AddField(fieldName, fieldContent, true);
                }
                _ = await (channel?.SendMessageAsync("", false, builder.Build()));
                if (latestUpdateUTC > DateTime.MinValue)
                {
                    guildFeed.LastUpdate = latestUpdateUTC;
                    _ = dbContext.Feeds.Update(guildFeed);
                    _ = await dbContext.SaveChangesAsync();
                }
            }
        }

        private string ParseHtml(string html)
        {
            if (html.IsEmptyOrWhiteSpace())
            {
                return string.Empty;
            }

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

            HtmlNodeCollection textNodes = htmlDoc?.DocumentNode?.SelectNodes("//text()");
            HtmlNodeCollection iframes = htmlDoc?.DocumentNode?.SelectNodes("//iframe[@src]");
            if (textNodes != null)
            {
                foreach (HtmlNode node in textNodes)
                {
                    if (!node.InnerText.IsEmptyOrWhiteSpace())
                    {
                        _ = sb.Append(node.InnerText.Trim() + "|");
                    }
                }
            }
            if (iframes != null)
            {
                foreach (HtmlNode node in iframes)
                {
                    if (node.HasAttributes &&
                        node.Attributes.Contains("src") &&
                        !node.Attributes["src"].Value.IsEmptyOrWhiteSpace())
                    {
                        _ = sb.Append(node.Attributes["src"].Value);
                    }
                }
            }
            string result = sb.ToString();
            return !result.IsEmpty() ? result : html;
        }
    }
}