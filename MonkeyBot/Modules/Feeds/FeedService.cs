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
        private static readonly TimeSpan _updateIntervall = TimeSpan.FromMinutes(30);
        private static readonly TimeSpan _startDelay = TimeSpan.FromSeconds(10);

        private readonly MonkeyDBContext _dbContext;
        private readonly DiscordClient _discordClient;
        private readonly ISchedulingService _schedulingService;
        private readonly ILogger<FeedService> _logger;

        public FeedService(MonkeyDBContext dbContext, DiscordClient discordClient, ISchedulingService schedulingService, ILogger<FeedService> logger)
        {
            _dbContext = dbContext;
            _discordClient = discordClient;
            _schedulingService = schedulingService;
            _logger = logger;
        }

        public void Start()
            => _schedulingService.ScheduleJobRecurring("feeds", _updateIntervall, async () => await GetAllFeedUpdatesAsync(), _startDelay);

        public async Task AddFeedAsync(string name, string url, ulong guildID, ulong channelID)
        {
            var feed = new Models.Feed
            {
                Name = name,
                URL = url,
                GuildID = guildID,
                ChannelID = channelID
            };
            _dbContext.Feeds.Add(feed);
            await _dbContext.SaveChangesAsync();
            await GetFeedUpdateAsync(feed, true);
        }

        public async Task RemoveFeedAsync(string nameOrUrl, ulong guildID)
        {
            var feeds = await _dbContext.Feeds.ToListAsync();
            Models.Feed feed = feeds.SingleOrDefault(f => f.Name.ToLowerInvariant() == nameOrUrl.ToLowerInvariant() || (f.URL.ToLowerInvariant() == nameOrUrl.ToLowerInvariant() && f.GuildID == guildID));
            if (feed != null)
            {
                _dbContext.Feeds.Remove(feed);
                await _dbContext.SaveChangesAsync();
            }
        }

        public async Task RemoveAllFeedsAsync(ulong guildID, ulong? channelID)
        {
            List<Models.Feed> allFeeds = await GetAllFeedsInternalAsync(guildID, channelID);
            if (allFeeds == null)
            {
                return;
            }
            _dbContext.Feeds.RemoveRange(allFeeds);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<List<GuildFeed>> GetFeedsForGuildAsync(ulong guildId, ulong? channelId = null)
        {
            List<Models.Feed> allFeeds = await GetAllFeedsInternalAsync(guildId, channelId);
            return allFeeds?.Select(x => new GuildFeed(x.Name, x.URL, x.ChannelID)).ToList();
        }

        public async Task<IEnumerable<string>> GetFeedUrls(string baseUrl)
            => (await FeedReader.GetFeedUrlsFromUrlAsync(baseUrl)).Select(x => x.Url);

        private Task<List<Models.Feed>> GetAllFeedsInternalAsync(ulong guildID, ulong? channelID = null)
        {
            return channelID.HasValue
                ? _dbContext.Feeds.AsQueryable().Where(x => x.GuildID == guildID && x.ChannelID == channelID.Value).ToListAsync()
                : _dbContext.Feeds.AsQueryable().Where(x => x.GuildID == guildID).ToListAsync();
        }

        private async Task GetAllFeedUpdatesAsync()
        {
            foreach (DiscordGuild guild in _discordClient?.Guilds.Values)
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
            if (!_discordClient.Guilds.TryGetValue(guildFeed.GuildID, out DiscordGuild guild))
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
                _logger.LogWarning(ex, $"Error getting feeds for {guildFeed.URL} in {guild.Name}:{channel.Name}");
                return;
            }
            if (feed == null || feed.Items == null || feed.Items.Count < 1)
            {
                return;
            }

            DateTime lastUpdateUTC = guildFeed.LastUpdate ?? DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(updateIntervallMinutes));
            IEnumerable<FeedItem> allFeeds = feed?.Items?.Where(x => x.PublishingDate.HasValue);
            var updatedFeeds = allFeeds?.Where(x => x.PublishingDate.Value.ToUniversalTime() > lastUpdateUTC)
                                        .OrderBy(x => x.PublishingDate)
                                        .ToList();
            if (updatedFeeds != null && updatedFeeds.Count == 0 && getLatest)
            {
                updatedFeeds = allFeeds.OrderByDescending(x => x.PublishingDate)
                                       .Take(1)
                                       .ToList();
            }
            if (updatedFeeds != null && updatedFeeds.Count > 0)
            {
                DateTime latestUpdateUTC = DateTime.MinValue;
                foreach (FeedItem feedItem in updatedFeeds)
                {
                    var builder = new DiscordEmbedBuilder();
                    builder.WithColor(DiscordColor.PhthaloGreen);

                    builder.WithTitle($"New update for \"{guildFeed.Name}".TruncateTo(255, "") + "\"");
                    var publishingDate = feedItem.PublishingDate.Value.ToUniversalTime();
                    if (feedItem.PublishingDate.HasValue && publishingDate > latestUpdateUTC)
                    {
                        latestUpdateUTC = publishingDate;
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
                    author = author?.Trim() ?? string.Empty;
                    (string content, string imgLink) = ParseHtml(feedItem.Description);
                    if (content.IsEmptyOrWhiteSpace())
                    {
                        (content, imgLink) = ParseHtml(feedItem.Content);
                    }
                    string title = feedItem.Title.Trim();
                    string description = title != content
                        ? $"{title}:\n{content}"
                        : content;
                    description = description.IsEmptyOrWhiteSpace() ? "[...]" : description.TruncateTo(500, $" [[...]]({feedItem.Link})");
                    builder.WithDescription(description);
                    builder.WithUrl(feedItem.Link);
                    builder.WithFooter($"{feed.Title} {publishingDate}", feed.ImageUrl);
                    if (!imgLink.IsEmptyOrWhiteSpace())
                    {
                        builder.WithImageUrl(imgLink);
                    }
                    await (channel?.SendMessageAsync(builder.Build()));
                }

                if (latestUpdateUTC > DateTime.MinValue)
                {
                    guildFeed.LastUpdate = latestUpdateUTC;
                    _dbContext.Feeds.Update(guildFeed);
                    await _dbContext.SaveChangesAsync();
                }
            }
        }

        private (string TextContent, string ImgLink) ParseHtml(string html)
        {
            if (html.IsEmptyOrWhiteSpace())
            {
                return new(string.Empty, string.Empty);
            }

            var htmlDoc = new HtmlDocument();

            try
            {
                html = MonkeyHelpers.CleanHtmlString(html);
                htmlDoc.LoadHtml(html);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing html");
                return new(html, string.Empty);
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
                        sb.Append(node.InnerText.Trim() + "|");
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
                        sb.Append(node.Attributes["src"].Value);
                    }
                }
            }
            string textContent = sb.Length > 0 ? sb.ToString().Trim('|') : html;

            var imgNodes = htmlDoc?
                .DocumentNode?
                .SelectNodes("//img");
            string imgLink =
                imgNodes?
                .Where(x => x.HasAttributes && x.Attributes.Contains("src") && !x.Attributes["src"].Value.IsEmptyOrWhiteSpace())?
                .Select(x => x.Attributes["src"].Value)?
                .Where(l => !l.EndsWith("gif"))?
                .FirstOrDefault() ?? string.Empty;

            return new(textContent.Trim(), imgLink);
        }
    }
}