using CodeHollow.FeedReader;
using CodeHollow.FeedReader.Feeds;
using Discord;
using Discord.WebSocket;
using dokas.FluentStrings;
using FluentScheduler;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using MonkeyBot.Common;
using MonkeyBot.Services.Common.Feeds;
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

        private readonly DbService dbService;
        private readonly DiscordSocketClient discordClient;
        private readonly ILogger<FeedService> logger;

        public FeedService(DbService dbService, DiscordSocketClient client, ILogger<FeedService> logger)
        {
            this.dbService = dbService;
            this.discordClient = client;
            this.logger = logger;
        }

        public void Start()
        {
            JobManager.AddJob(async () => await GetAllFeedUpdatesAsync(), (x) => x.ToRunNow().AndEvery(updateIntervallMinutes).Minutes().DelayFor(10).Seconds());
        }

        public async Task AddFeedAsync(string url, ulong guildId, ulong channelId)
        {
            var feed = new FeedDTO
            {
                URL = url,
                GuildId = guildId,
                ChannelId = channelId
            };
            using (var uow = dbService.UnitOfWork)
            {
                await uow.Feeds.AddOrUpdateAsync(feed);
                await uow.CompleteAsync();
            }
            await GetFeedUpdateAsync(feed, true);
        }

        public async Task RemoveFeedAsync(string url, ulong guildId, ulong channelId)
        {
            var feed = new FeedDTO
            {
                URL = url,
                GuildId = guildId,
                ChannelId = channelId
            };
            using (var uow = dbService.UnitOfWork)
            {
                await uow.Feeds.RemoveAsync(feed);
                await uow.CompleteAsync();
            }
        }

        public async Task RemoveAllFeedsAsync(ulong guildId, ulong? channelId)
        {
            List<FeedDTO> allFeeds = await GetAllFeedsInternalAsync(guildId, channelId);
            if (allFeeds == null)
                return;
            using (var uow = dbService.UnitOfWork)
            {
                foreach (var feed in allFeeds)
                {
                    await uow.Feeds.RemoveAsync(feed);
                }
                await uow.CompleteAsync();
            }
        }

        public async Task<List<(ulong feedChannelId, string feedUrl)>> GetFeedUrlsForGuildAsync(ulong guildId, ulong? channelId = null)
        {
            List<FeedDTO> allFeeds = await GetAllFeedsInternalAsync(guildId, channelId);
            return allFeeds?.Select(x => (x.ChannelId, x.URL)).ToList();
        }

        private async Task<List<FeedDTO>> GetAllFeedsInternalAsync(ulong guildId, ulong? channelId = null)
        {
            List<FeedDTO> allFeeds = null;
            using (var uow = dbService.UnitOfWork)
            {
                if (channelId.HasValue)
                    allFeeds = await uow.Feeds.GetAllForGuildAsync(guildId, x => x.ChannelId == channelId);
                else
                    allFeeds = await uow.Feeds.GetAllForGuildAsync(guildId);
            }
            return allFeeds;
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
            var feeds = await GetAllFeedsInternalAsync(guild.Id);
            foreach (var feed in feeds)
            {
                await GetFeedUpdateAsync(feed);
            }
        }

        private async Task GetFeedUpdateAsync(FeedDTO guildFeed, bool getLatest = false)
        {
            var guild = discordClient.GetGuild(guildFeed.GuildId);
            var channel = guild?.GetTextChannel(guildFeed.ChannelId);
            if (guild == null || channel == null || guildFeed.URL.IsEmpty())
                return;
            Feed feed;
            try
            {
                feed = await FeedReader.ReadAsync(guildFeed.URL);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error getting feeds");
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
                string title = $"New update{(updatedFeeds.Count > 1 ? "s" : "")} for \"{ParseHtml(feed.Title) ?? guildFeed.URL}".TruncateTo(255) + "\"";
                builder.WithTitle(title);
                DateTime latestUpdateUTC = DateTime.MinValue;
                foreach (var feedItem in updatedFeeds)
                {
                    if (feedItem.PublishingDate.HasValue && feedItem.PublishingDate.Value.ToUniversalTime() > latestUpdateUTC)
                        latestUpdateUTC = feedItem.PublishingDate.Value.ToUniversalTime();
                    string fieldName = feedItem.PublishingDate.HasValue ? feedItem.PublishingDate.Value.ToLocalTime().ToString() : DateTime.Now.ToString();
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
                    builder.AddField(fieldName, fieldContent, true);
                }
                await channel?.SendMessageAsync("", false, builder.Build());
                if (latestUpdateUTC > DateTime.MinValue)
                {
                    guildFeed.LastUpdate = latestUpdateUTC;
                    using (var uow = dbService.UnitOfWork)
                    {
                        await uow.Feeds.AddOrUpdateAsync(guildFeed);
                        await uow.CompleteAsync();
                    }
                }
            }
        }

        private string ParseHtml(string html)
        {
            if (html.IsEmpty().OrWhiteSpace())
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
                    if (node.HasAttributes &&
                        node.Attributes.Contains("src") &&
                        !node.Attributes["src"].Value.IsEmpty().OrWhiteSpace())
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