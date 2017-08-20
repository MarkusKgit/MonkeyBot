using CodeHollow.FeedReader;
using Discord;
using Discord.WebSocket;
using FluentScheduler;
using Microsoft.Extensions.DependencyInjection;
using MonkeyBot.Common.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MonkeyBot.Services
{
    public class BackgroundService : IBackgroundService
    {
        private const int updateIntervallMinutes = 60;

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

            var feed = await FeedReader.ReadAsync(feedUrl);
            var now = DateTime.Now.ToUniversalTime();
            var updatedFeeds = feed?.Items?.Where(x => x.PublishingDate.HasValue && (x.PublishingDate.Value.ToUniversalTime() > now.Subtract(TimeSpan.FromMinutes(updateIntervallMinutes)))).ToList();
            if (updatedFeeds != null && updatedFeeds.Count > 0)
            {
                var builder = new EmbedBuilder();
                builder.WithColor(new Color(21, 26, 35));
                string title = $"New update{(updatedFeeds.Count > 1 ? "s" : "")} for \"{ParseHtml(feed.Title) ?? feedUrl}".Truncate(255) + "\"";
                builder.WithTitle(title);
                foreach (var feedItem in updatedFeeds)
                {
                    string date = feedItem.PublishingDate.HasValue ? feedItem.PublishingDate.ToString() : feedItem.PublishingDateString;
                    string maskedLink = $"[{ParseHtml(feedItem.Title)}]({feedItem.Link})";
                    string description = $"{maskedLink}{Environment.NewLine}*{ParseHtml(feedItem.Description) ?? string.Empty}".Truncate(1023) + "*"; // Embed field value must be <= 1024 characters
                    builder.AddInlineField(date, description);
                }
                await channel?.SendMessageAsync("", false, builder.Build());
            }
        }

        private string ParseHtml(string html)
        {
            if (string.IsNullOrEmpty(html))
                return html;
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