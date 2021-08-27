using DSharpPlus;
using DSharpPlus.Entities;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using MonkeyBot.Models;
using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace MonkeyBot.Services
{
    public class BattlefieldNewsService : IBattlefieldNewsService
    {
        private const int updateIntervallSeconds = 30 * 60;

        private readonly DiscordClient discordClient;
        private readonly IGuildService guildService;
        private readonly ISchedulingService schedulingService;
        private readonly ILogger<BattlefieldNewsService> logger;

        public BattlefieldNewsService(DiscordClient discordClient, IGuildService guildService, ISchedulingService schedulingService, ILogger<BattlefieldNewsService> logger)
        {
            this.discordClient = discordClient;
            this.guildService = guildService;
            this.schedulingService = schedulingService;
            this.logger = logger;
        }

        public void Start()
            => schedulingService.ScheduleJobRecurring("battlefieldNews", updateIntervallSeconds, async () => await GetUpdatesAsync(), 10);

        public async Task EnableForGuildAsync(ulong guildID, ulong channelID)
        {
            if (!discordClient.Guilds.TryGetValue(guildID, out DiscordGuild guild))
            {
                return;
            }
            GuildConfig cfg = await guildService.GetOrCreateConfigAsync(guildID);
            cfg.BattlefieldUpdatesEnabled = true;
            cfg.BattlefieldUpdatesChannel = channelID;
            await guildService.UpdateConfigAsync(cfg);
            await GetUpdateForGuildAsync(await GetLatestBattlefieldUpdateAsync(), guild);
        }

        public async Task DisableForGuildAsync(ulong guildID)
        {
            GuildConfig cfg = await guildService.GetOrCreateConfigAsync(guildID);
            if (cfg.BattlefieldUpdatesEnabled)
            {
                cfg.BattlefieldUpdatesEnabled = false;
                cfg.LastBattlefieldUpdate = null;
                await guildService.UpdateConfigAsync(cfg);
            }
        }

        private async Task GetUpdatesAsync()
        {
            BattlefieldUpdate latestBattlefieldVUpdate = await GetLatestBattlefieldUpdateAsync();

            foreach (DiscordGuild guild in discordClient?.Guilds.Values)
            {
                await GetUpdateForGuildAsync(latestBattlefieldVUpdate, guild);
            }
        }

        private async Task GetUpdateForGuildAsync(BattlefieldUpdate latestBattlefieldUpdate, DiscordGuild guild)
        {
            GuildConfig cfg = await guildService.GetOrCreateConfigAsync(guild.Id);
            if (cfg == null || !cfg.BattlefieldUpdatesEnabled)
            {
                return;
            }
            DiscordChannel channel = guild.GetChannel(cfg.BattlefieldUpdatesChannel);
            if (channel == null)
            {
                logger.LogError($"Battlefield Updates enabled for {guild.Name} but channel {cfg.BattlefieldUpdatesChannel} is invalid");
                return;
            }
            DateTime lastUpdateUTC = cfg.LastBattlefieldUpdate ?? DateTime.MinValue;
            if (lastUpdateUTC >= latestBattlefieldUpdate.UpdateDate)
            {
                return;
            }
            var builder = new DiscordEmbedBuilder()
                .WithColor(new DiscordColor(21, 26, 35))
                .WithTitle("New Battlefield Update")
                .WithDescription($"[{latestBattlefieldUpdate.Title}]({latestBattlefieldUpdate.UpdateUrl})\n{latestBattlefieldUpdate.Description}")
                .WithImageUrl(latestBattlefieldUpdate.ImgUrl)
                .WithFooter(latestBattlefieldUpdate.UpdateDate.ToString());
            _ = await (channel?.SendMessageAsync(builder.Build()));

            cfg.LastBattlefieldUpdate = latestBattlefieldUpdate.UpdateDate;
            await guildService.UpdateConfigAsync(cfg);
        }

        private static async Task<BattlefieldUpdate> GetLatestBattlefieldUpdateAsync()
        {
            var web = new HtmlWeb();
            HtmlDocument document = await web.LoadFromWebAsync("https://www.battlefield.com/en-gb/news");
            HtmlNode latestUpdate = document?.DocumentNode?.SelectNodes("//ea-grid/ea-container/ea-tile")?.FirstOrDefault();
            string imgUrl = latestUpdate?.GetAttributeValue<string>("media", "");
            string title = latestUpdate?.SelectNodes(".//h3")?.FirstOrDefault()?.InnerHtml.Trim();
            string description = latestUpdate?.SelectSingleNode(".//ea-tile-copy")?.InnerHtml?.Trim();
            string sUpdateDate = latestUpdate?.SelectNodes(".//div")?.LastOrDefault()?.InnerHtml;
            string link = latestUpdate?.SelectSingleNode(".//ea-cta")?.Attributes["link-url"]?.Value;
            return  !string.IsNullOrEmpty(imgUrl)
                    && !string.IsNullOrEmpty(title)
                    && !string.IsNullOrEmpty(description)
                    && !string.IsNullOrEmpty(sUpdateDate)
                    && !string.IsNullOrEmpty(link)
                    && DateTime.TryParseExact(sUpdateDate, "dd-MMM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDate)
                ? new BattlefieldUpdate (imgUrl, title, description, $"https://www.battlefield.com{link}", parsedDate.ToUniversalTime())
                : null;
        }
    }
}
