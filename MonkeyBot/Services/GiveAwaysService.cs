using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;
using MonkeyBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace MonkeyBot.Services
{
    public class GiveAwaysService : IGiveAwaysService
    {
        private static readonly TimeSpan _updateIntervall = TimeSpan.FromHours(1);
        private static readonly TimeSpan _startDelay = TimeSpan.FromSeconds(10);
        private static readonly Uri giveAwaysApiUri = new("https://www.gamerpower.com/api/giveaways?platform=pc&type=game");

        private readonly DiscordClient _discordClient;
        private readonly IHttpClientFactory _clientFactory;
        private readonly IGuildService _guildService;
        private readonly ISchedulingService _schedulingService;
        private readonly ILogger<GiveAwaysService> _logger;

        public GiveAwaysService(DiscordClient discordClient, IHttpClientFactory clientFactory, IGuildService guildService, ISchedulingService schedulingService, ILogger<GiveAwaysService> logger)
        {
            _discordClient = discordClient;
            _clientFactory = clientFactory;
            _guildService = guildService;
            _schedulingService = schedulingService;
            _logger = logger;
        }

        public void Start()
            => _schedulingService.ScheduleJobRecurring("giveAways", _updateIntervall, async () => await GetUpdatesAsync(), _startDelay);

        public async Task EnableForGuildAsync(ulong guildID, ulong channelID)
        {
            if (!_discordClient.Guilds.TryGetValue(guildID, out DiscordGuild guild))
            {
                return;
            }
            GuildConfig cfg = await _guildService.GetOrCreateConfigAsync(guildID);
            cfg.GiveAwaysEnabled = true;
            cfg.GiveAwayChannel = channelID;
            await _guildService.UpdateConfigAsync(cfg);
            var latestUpdate = (await GetLatestGiveAwaysAsync()).Take(1).ToList();
            await GetUpdateForGuildAsync(latestUpdate, guild);
        }

        public async Task DisableForGuildAsync(ulong guildID)
        {
            GuildConfig cfg = await _guildService.GetOrCreateConfigAsync(guildID);
            if (cfg.GiveAwaysEnabled)
            {
                cfg.GiveAwaysEnabled = false;
                cfg.GiveAwayChannel = 0;
                cfg.LastGiveAway = null;
                await _guildService.UpdateConfigAsync(cfg);
            }
        }

        private async Task GetUpdatesAsync()
        {
            List<GiveAway> latestgiveAways = (await GetLatestGiveAwaysAsync()).Reverse().ToList();

            foreach (DiscordGuild guild in _discordClient?.Guilds.Values)
            {
                await GetUpdateForGuildAsync(latestgiveAways, guild);
            }
        }

        private async Task GetUpdateForGuildAsync(List<GiveAway> latestgiveAways, DiscordGuild guild)
        {
            GuildConfig cfg = await _guildService.GetOrCreateConfigAsync(guild.Id);
            if (cfg == null || !cfg.GiveAwaysEnabled)
            {
                return;
            }
            DiscordChannel channel = guild.GetChannel(cfg.GiveAwayChannel);
            if (channel == null)
            {
                _logger.LogError($"Giveaways enabled for {guild.Name} but channel {cfg.BattlefieldUpdatesChannel} is invalid");
                return;
            }

            if (latestgiveAways.Count > 1 && cfg.LastGiveAway.HasValue)
            {
                latestgiveAways = latestgiveAways.Where(x => x.PublishedDate.ToUniversalTime() > cfg.LastGiveAway.Value).ToList();
            }

            DateTime latestUpdateUTC = DateTime.MinValue;
            foreach (var giveAway in latestgiveAways)
            {
                var builder = new DiscordEmbedBuilder();
                builder.WithColor(DiscordColor.DarkGreen);

                var publishingDate = giveAway.PublishedDate.ToUniversalTime();
                if (publishingDate > latestUpdateUTC)
                {
                    latestUpdateUTC = publishingDate;
                }

                string title = $"Free: {giveAway.Title}";
                if (giveAway.Worth.StartsWith("$"))
                {
                    title += $" ({Formatter.Strike(giveAway.Worth)})";
                }
                builder.WithTitle(title);
                builder.WithImageUrl(giveAway.ImageUrl);
                builder.WithDescription(giveAway.Description);
                builder.WithUrl(giveAway.GiveAwayUrl);
                if (!giveAway.Instructions.IsEmptyOrWhiteSpace())
                {
                    builder.AddField("Instructions", giveAway.Instructions);
                }
                builder.AddField("Link", $"[{giveAway.GiveAwayUrl}]({giveAway.GiveAwayUrl})");
                string footer = $"Published: {giveAway.PublishedDate:g}";
                if (giveAway.EndDate.HasValue)
                {
                    footer += $" - valid until: {giveAway.EndDate.Value:d}";
                }
                builder.WithFooter(footer);

                await (channel?.SendMessageAsync(builder.Build()));
            }

            if (latestUpdateUTC > DateTime.MinValue)
            {
                cfg.LastGiveAway = latestUpdateUTC;
                await _guildService.UpdateConfigAsync(cfg);
            }
        }

        private async Task<IEnumerable<GiveAway>> GetLatestGiveAwaysAsync()
        {
            var httpClient = _clientFactory.CreateClient();
            try
            {
                var giveaways = await httpClient.GetFromJsonAsync<GiveAway[]>(giveAwaysApiUri);
                return giveaways;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while trying to get latest giveaways");
                return Enumerable.Empty<GiveAway>();
            }
        }
    }
}