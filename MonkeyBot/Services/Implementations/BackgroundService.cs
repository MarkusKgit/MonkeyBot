using Discord;
using Discord.WebSocket;
using FluentScheduler;
using Microsoft.Extensions.DependencyInjection;
using MonkeyBot.Services.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MonkeyBot.Services
{
    public class BackgroundService : IBackgroundService
    {
        private const int updateIntervallMinutes = 30;

        private DbService db;
        private DiscordSocketClient client;

        public BackgroundService(IServiceProvider provider)
        {
            db = provider.GetService<DbService>();
            client = provider.GetService<DiscordSocketClient>();
        }

        public void Start()
        {
            JobManager.AddJob(async () => await GetAllFeedUpdatesAsync(), (x) => x.ToRunNow().AndEvery(updateIntervallMinutes).Minutes().DelayFor(10).Seconds());
        }

        public async Task RunOnceAllFeedsAsync(ulong guildId)
        {
            var guild = client.GetGuild(guildId);
            if (guild != null)
                await GetGuildFeedUpdates(guild);
        }

        public async Task RunOnceSingleFeedAsync(ulong guildId, ulong channelId, string url)
        {
            var guild = client.GetGuild(guildId);
            var channel = guild?.GetTextChannel(channelId);
            if (guild != null && channel != null)
                await GetFeedUpdate(channel, url);
        }

        private async Task GetAllFeedUpdatesAsync()
        {
            foreach (var guild in client?.Guilds)
            {
                await GetGuildFeedUpdates(guild);
            }
        }

        private async Task GetGuildFeedUpdates(SocketGuild guild)
        {
            List<string> feedUrls = null;
            SocketTextChannel channel = null;

            using (var uow = db?.UnitOfWork)
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
                await GetFeedUpdate(channel, feedUrl);
            }
        }

        private async Task GetFeedUpdate(SocketTextChannel channel, string feedUrl)
        {
            if (channel == null || string.IsNullOrEmpty(feedUrl))
                return;
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(feedUrl);
                var responseMessage = await client.GetAsync(feedUrl);
                var responseString = await responseMessage.Content.ReadAsStringAsync();

                //extract feed items
                XDocument doc = XDocument.Parse(responseString);
                var rootName = doc.Root.Name.LocalName.ToLowerInvariant();
                IEnumerable<FeedItem> feedItems = null;
                if (rootName == "rss")
                {
                    feedItems = ParseRss(doc);
                }
                else if (rootName == "feed")
                {
                    feedItems = ParseAtom(doc);
                }
                // Only list feeds that have been updated since the last check
                var updatedFeeds = feedItems?.Where(x => x.PublishDate > DateTime.Now.Subtract(TimeSpan.FromMinutes(updateIntervallMinutes))).ToList();
                if (updatedFeeds != null && updatedFeeds.Count > 0)
                {
                    var builder = new EmbedBuilder();
                    builder.WithColor(new Color(21, 26, 35));
                    builder.WithTitle($"New update{(updatedFeeds.Count > 1 ? "s" : "")} for {feedUrl}");
                    foreach (var feedItem in updatedFeeds)
                    {
                        string title = feedItem.Title;
                        string maskedLink = $"[{title}]({feedItem.Link})";
                        string content = feedItem.Content;
                        string description = $"{maskedLink}{Environment.NewLine}*{content}*";
                        builder.AddInlineField(feedItem.PublishDate.ToString(), description);
                    }
                    await channel?.SendMessageAsync("", false, builder.Build());
                }
            }
        }

        private IEnumerable<FeedItem> ParseAtom(XDocument doc)
        {
            try
            {
                var entries = from item in doc.Root.Elements().Where(i => i.Name.LocalName == "entry")
                              select new FeedItem
                              {
                                  Content = item.Elements().First(i => i.Name.LocalName == "content").Value,
                                  Link = item.Elements().First(i => i.Name.LocalName == "link").Attribute("href").Value,
                                  PublishDate = ParseDate(item.Elements().First(i => i.Name.LocalName == "published").Value),
                                  Title = item.Elements().First(i => i.Name.LocalName == "title").Value
                              };
                return entries;
            }
            catch
            {
                return null;
            }
        }

        private IEnumerable<FeedItem> ParseRss(XDocument doc)
        {
            try
            {
                var entries = from item in doc.Root.Descendants().FirstOrDefault(i => i.Name.LocalName == "channel")?.Elements()?.Where(i => i.Name.LocalName == "item")
                              select new FeedItem
                              {
                                  Content = ParseHtml(item.Elements().FirstOrDefault(i => i.Name.LocalName == "description")?.Value),
                                  Link = ParseHtml(item.Elements().FirstOrDefault(i => i.Name.LocalName == "link")?.Value),
                                  PublishDate = ParseDate(item.Elements().FirstOrDefault(i => i.Name.LocalName == "pubDate")?.Value),
                                  Title = ParseHtml(item.Elements().FirstOrDefault(i => i.Name.LocalName == "title")?.Value)
                              };
                return entries;
            }
            catch
            {
                return null;
            }

        }

        private DateTime ParseDate(string date)
        {
            DateTime result;
            if (DateTime.TryParse(date, out result))
                return result;
            else
                return DateTime.MinValue;
        }

        private string ParseHtml(string html)
        {
            string result = WebUtility.HtmlDecode(html);
            result = result.Replace("<b>", "**").Replace("</b>", "**");
            result = result.Replace("<i>", "*").Replace("</i>", "*");
            string regex = "<(?:\"[^ \"]*\"['\"]*|'[^ ']*'['\"]*|[^'\">])+>";
            result = Regex.Replace(result, regex, "");
            result = result.Trim('\n').Trim('\t').Trim();
            return result;
        }
    }
}