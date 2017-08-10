using Discord.WebSocket;
using FluentScheduler;
using Microsoft.Extensions.DependencyInjection;
using MonkeyBot.Services.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace MonkeyBot.Services
{
    public class BackgroundService : IBackgroundService
    {
        private DbService db;
        private DiscordSocketClient client;

        public BackgroundService(IServiceProvider provider)
        {
            db = provider.GetService<DbService>();
            client = provider.GetService<DiscordSocketClient>();
        }

        public void Start()
        {
            JobManager.AddJob(async () => await GetForumUpdatesAsync(), (x) => x.ToRunEvery(30).Minutes());
        }

        private async Task GetForumUpdatesAsync()
        {
            foreach (var guild in client?.Guilds)
            {
                string feedUrl = string.Empty;
                using (var uow = db?.UnitOfWork)
                {
                    var cfg = await uow.GuildConfigs.GetAsync(guild.Id);
                    if (cfg == null || !cfg.ListenToFeed || string.IsNullOrEmpty(feedUrl))
                        continue;
                    feedUrl = cfg.Feedurl;
                }
                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri(feedUrl);
                    var responseMessage = await client.GetAsync(feedUrl);
                    var responseString = await responseMessage.Content.ReadAsStringAsync();

                    //extract feed items
                    XDocument doc = XDocument.Parse(responseString);
                    var feedItems = from item in doc.Root.Descendants().First(i => i.Name.LocalName == "channel").Elements().Where(i => i.Name.LocalName == "item")
                                    select new FeedItem
                                    {
                                        Content = item.Elements().First(i => i.Name.LocalName == "description").Value,
                                        Link = item.Elements().First(i => i.Name.LocalName == "link").Value,
                                        PublishDate = ParseDate(item.Elements().First(i => i.Name.LocalName == "pubDate").Value),
                                        Title = item.Elements().First(i => i.Name.LocalName == "title").Value
                                    };
                    var articles = feedItems.ToList();
                }
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
    }
}
