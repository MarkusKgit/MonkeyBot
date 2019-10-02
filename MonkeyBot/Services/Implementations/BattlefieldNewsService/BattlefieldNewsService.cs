using Discord;
using Discord.WebSocket;
using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MonkeyBot.Database;
using MonkeyBot.Models;
using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace MonkeyBot.Services
{
    public class BattlefieldNewsService : IBattlefieldNewsService
    {
        private const int updateIntervallSeconds = 24*60*60; //once per day

        private readonly MonkeyDBContext dbContext;
        private readonly DiscordSocketClient discordClient;
        private readonly ISchedulingService schedulingService;
        private readonly ILogger<BattlefieldNewsService> logger;

        public BattlefieldNewsService(MonkeyDBContext dbContext, DiscordSocketClient discordClient, ISchedulingService schedulingService, ILogger<BattlefieldNewsService> logger)
        {
            this.dbContext = dbContext;
            this.discordClient = discordClient;
            this.schedulingService = schedulingService;
            this.logger = logger;
        }

        public void Start()
        {
            schedulingService.ScheduleJobRecurring("battlefieldNews", updateIntervallSeconds, async () => await GetUpdatesAsync().ConfigureAwait(false), 10);
        }

        public async Task EnableForGuildAsync(ulong guildID, ulong channelID)
        {
            var cfg = await dbContext.GuildConfigs.SingleOrDefaultAsync(g => g.GuildID == guildID).ConfigureAwait(false) ?? new GuildConfig { GuildID = guildID };
            cfg.BattlefieldUpdatesEnabled = true;
            cfg.BattlefieldUpdatesChannel = channelID;
            dbContext.GuildConfigs.Update(cfg);
            await dbContext.SaveChangesAsync().ConfigureAwait(false);

            await GetUpdateForGuildAsync(await GetLatestBattlefieldVUpdateAsync().ConfigureAwait(false), discordClient.GetGuild(guildID)).ConfigureAwait(false);
        }

        public async Task DisableForGuildAsync(ulong guildID)
        {
            var cfg = await dbContext.GuildConfigs.SingleOrDefaultAsync(g => g.GuildID == guildID).ConfigureAwait(false);
            if (cfg != null)
            {
                cfg.BattlefieldUpdatesEnabled = false;
                dbContext.GuildConfigs.Update(cfg);
                await dbContext.SaveChangesAsync().ConfigureAwait(false);
            }
        }

        private async Task GetUpdatesAsync()
        {
            var latestBattlefieldVUpdate = await GetLatestBattlefieldVUpdateAsync().ConfigureAwait(false);

            foreach (var guild in discordClient?.Guilds)
            {
                await GetUpdateForGuildAsync(latestBattlefieldVUpdate, guild).ConfigureAwait(false);
            }
        }

        private async Task GetUpdateForGuildAsync(BattlefieldVUpdate latestBattlefieldVUpdate, SocketGuild guild)
        {
            var cfg = await dbContext.GuildConfigs.SingleOrDefaultAsync(g => g.GuildID == guild.Id).ConfigureAwait(false);
            if (cfg == null || !cfg.BattlefieldUpdatesEnabled)
            {
                return;
            }
            var channel = guild.GetTextChannel(cfg.BattlefieldUpdatesChannel);
            if (channel == null)
            {
                logger.LogError($"Battlefield Updates enabled for {guild.Name} but channel {cfg.BattlefieldUpdatesChannel} is invalid");
                return;
            }
            var lastUpdateUTC = cfg.LastBattlefieldUpdate ?? DateTime.MinValue;
            if (lastUpdateUTC >= latestBattlefieldVUpdate.UpdateDate)
            {
                return;
            }
            var builder = new EmbedBuilder();
            builder.WithColor(new Color(21, 26, 35));
            builder.WithTitle("New Battlefield V Update");
            builder.WithDescription($"[{latestBattlefieldVUpdate.Title}]({latestBattlefieldVUpdate.UpdateUrl})");
            builder.WithFooter(latestBattlefieldVUpdate.UpdateDate.ToString());
            await (channel?.SendMessageAsync("", false, builder.Build())).ConfigureAwait(false);

            cfg.LastBattlefieldUpdate = latestBattlefieldVUpdate.UpdateDate;
            dbContext.GuildConfigs.Update(cfg);
            await dbContext.SaveChangesAsync().ConfigureAwait(false);
        }

        private static async Task<BattlefieldVUpdate> GetLatestBattlefieldVUpdateAsync()
        {
            var web = new HtmlWeb();
            var document = await web.LoadFromWebAsync("https://www.battlefield.com/en-gb/news").ConfigureAwait(false);
            var latestUpdate = document?.DocumentNode?.SelectNodes("//ea-grid/ea-container/ea-tile")?.FirstOrDefault();
            var title = latestUpdate?.SelectNodes(".//h3")?.FirstOrDefault()?.InnerHtml;
            var sUpdateDate = latestUpdate?.SelectNodes(".//div")?.LastOrDefault()?.InnerHtml;
            var link = latestUpdate?.SelectNodes(".//ea-cta")?.FirstOrDefault()?.Attributes["link-url"]?.Value;
            if (!string.IsNullOrEmpty(title)
                && !string.IsNullOrEmpty(sUpdateDate)
                && !string.IsNullOrEmpty(link)
                && DateTime.TryParseExact(sUpdateDate, "dd-MMM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
            {
                return new BattlefieldVUpdate { Title = title, UpdateUrl = $"https://www.battlefield.com{link}", UpdateDate = parsedDate.ToUniversalTime()};
            }
            return null;
        }
    }
}
