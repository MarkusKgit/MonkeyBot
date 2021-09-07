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
        private static readonly TimeSpan _updateIntervall = TimeSpan.FromHours(1);
        private static readonly TimeSpan _startDelay = TimeSpan.FromSeconds(10);

        private readonly DiscordClient _discordClient;
        private readonly IGuildService _guildService;
        private readonly ISchedulingService _schedulingService;
        private readonly ILogger<BattlefieldNewsService> _logger;

        public BattlefieldNewsService(DiscordClient discordClient, IGuildService guildService, ISchedulingService schedulingService, ILogger<BattlefieldNewsService> logger)
        {
            _discordClient = discordClient;
            _guildService = guildService;
            _schedulingService = schedulingService;
            _logger = logger;
        }

        public void Start()
            => _schedulingService.ScheduleJobRecurring("battlefieldNews", _updateIntervall, async () => await GetUpdatesAsync(), _startDelay);

        public async Task EnableForGuildAsync(ulong guildID, ulong channelID)
        {
            if (!_discordClient.Guilds.TryGetValue(guildID, out DiscordGuild guild))
            {
                return;
            }
            GuildConfig cfg = await _guildService.GetOrCreateConfigAsync(guildID);
            cfg.BattlefieldUpdatesEnabled = true;
            cfg.BattlefieldUpdatesChannel = channelID;
            await _guildService.UpdateConfigAsync(cfg);
            await GetUpdateForGuildAsync(await GetLatestBattlefieldUpdateAsync(), guild);
        }

        public async Task DisableForGuildAsync(ulong guildID)
        {
            GuildConfig cfg = await _guildService.GetOrCreateConfigAsync(guildID);
            if (cfg.BattlefieldUpdatesEnabled)
            {
                cfg.BattlefieldUpdatesEnabled = false;
                cfg.LastBattlefieldUpdate = null;
                await _guildService.UpdateConfigAsync(cfg);
            }
        }

        private async Task GetUpdatesAsync()
        {
            BattlefieldUpdate latestBattlefieldVUpdate = await GetLatestBattlefieldUpdateAsync();

            foreach (DiscordGuild guild in _discordClient?.Guilds.Values)
            {
                await GetUpdateForGuildAsync(latestBattlefieldVUpdate, guild);
            }
        }

        private async Task GetUpdateForGuildAsync(BattlefieldUpdate latestBattlefieldUpdate, DiscordGuild guild)
        {
            GuildConfig cfg = await _guildService.GetOrCreateConfigAsync(guild.Id);
            if (cfg == null || !cfg.BattlefieldUpdatesEnabled)
            {
                return;
            }
            DiscordChannel channel = guild.GetChannel(cfg.BattlefieldUpdatesChannel);
            if (channel == null)
            {
                _logger.LogError($"Battlefield Updates enabled for {guild.Name} but channel {cfg.BattlefieldUpdatesChannel} is invalid");
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
            await (channel?.SendMessageAsync(builder.Build()));

            cfg.LastBattlefieldUpdate = latestBattlefieldUpdate.UpdateDate;
            await _guildService.UpdateConfigAsync(cfg);
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
