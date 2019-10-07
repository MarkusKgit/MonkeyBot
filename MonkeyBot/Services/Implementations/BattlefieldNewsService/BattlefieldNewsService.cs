﻿using Discord;
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
        private const int updateIntervallSeconds = 30 * 60;

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
            => schedulingService.ScheduleJobRecurring("battlefieldNews", updateIntervallSeconds, async () => await GetUpdatesAsync().ConfigureAwait(false), 10);

        public async Task EnableForGuildAsync(ulong guildID, ulong channelID)
        {
            GuildConfig cfg = await dbContext.GuildConfigs.SingleOrDefaultAsync(g => g.GuildID == guildID).ConfigureAwait(false) 
                ?? new GuildConfig { GuildID = guildID };
            cfg.BattlefieldUpdatesEnabled = true;
            cfg.BattlefieldUpdatesChannel = channelID;
            _ = dbContext.GuildConfigs.Update(cfg);
            _ = await dbContext.SaveChangesAsync().ConfigureAwait(false);

            await GetUpdateForGuildAsync(await GetLatestBattlefieldVUpdateAsync().ConfigureAwait(false), discordClient.GetGuild(guildID)).ConfigureAwait(false);
        }

        public async Task DisableForGuildAsync(ulong guildID)
        {
            GuildConfig cfg = await dbContext.GuildConfigs.SingleOrDefaultAsync(g => g.GuildID == guildID).ConfigureAwait(false);
            if (cfg != null)
            {
                cfg.BattlefieldUpdatesEnabled = false;
                _ = dbContext.GuildConfigs.Update(cfg);
                _ = await dbContext.SaveChangesAsync().ConfigureAwait(false);
            }
        }

        private async Task GetUpdatesAsync()
        {
            BattlefieldVUpdate latestBattlefieldVUpdate = await GetLatestBattlefieldVUpdateAsync().ConfigureAwait(false);

            foreach (SocketGuild guild in discordClient?.Guilds)
            {
                await GetUpdateForGuildAsync(latestBattlefieldVUpdate, guild).ConfigureAwait(false);
            }
        }

        private async Task GetUpdateForGuildAsync(BattlefieldVUpdate latestBattlefieldVUpdate, SocketGuild guild)
        {
            GuildConfig cfg = await dbContext.GuildConfigs.SingleOrDefaultAsync(g => g.GuildID == guild.Id).ConfigureAwait(false);
            if (cfg == null || !cfg.BattlefieldUpdatesEnabled)
            {
                return;
            }
            SocketTextChannel channel = guild.GetTextChannel(cfg.BattlefieldUpdatesChannel);
            if (channel == null)
            {
                logger.LogError($"Battlefield Updates enabled for {guild.Name} but channel {cfg.BattlefieldUpdatesChannel} is invalid");
                return;
            }
            DateTime lastUpdateUTC = cfg.LastBattlefieldUpdate ?? DateTime.MinValue;
            if (lastUpdateUTC >= latestBattlefieldVUpdate.UpdateDate)
            {
                return;
            }
            var builder = new EmbedBuilder()
                .WithColor(new Color(21, 26, 35))
                .WithTitle("New Battlefield V Update")
                .WithDescription($"[{latestBattlefieldVUpdate.Title}]({latestBattlefieldVUpdate.UpdateUrl})")
                .WithFooter(latestBattlefieldVUpdate.UpdateDate.ToString());
            _ = await (channel?.SendMessageAsync("", false, builder.Build())).ConfigureAwait(false);

            cfg.LastBattlefieldUpdate = latestBattlefieldVUpdate.UpdateDate;
            _ = dbContext.GuildConfigs.Update(cfg);
            _ = await dbContext.SaveChangesAsync().ConfigureAwait(false);
        }

        private static async Task<BattlefieldVUpdate> GetLatestBattlefieldVUpdateAsync()
        {
            var web = new HtmlWeb();
            HtmlDocument document = await web.LoadFromWebAsync("https://www.battlefield.com/en-gb/news").ConfigureAwait(false);
            HtmlNode latestUpdate = document?.DocumentNode?.SelectNodes("//ea-grid/ea-container/ea-tile")?.FirstOrDefault();
            string title = latestUpdate?.SelectNodes(".//h3")?.FirstOrDefault()?.InnerHtml;
            string sUpdateDate = latestUpdate?.SelectNodes(".//div")?.LastOrDefault()?.InnerHtml;
            string link = latestUpdate?.SelectNodes(".//ea-cta")?.FirstOrDefault()?.Attributes["link-url"]?.Value;
            return !string.IsNullOrEmpty(title)
                    && !string.IsNullOrEmpty(sUpdateDate)
                    && !string.IsNullOrEmpty(link)
                    && DateTime.TryParseExact(sUpdateDate, "dd-MMM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDate)
                ? new BattlefieldVUpdate { Title = title, UpdateUrl = $"https://www.battlefield.com{link}", UpdateDate = parsedDate.ToUniversalTime() }
                : null;
        }
    }
}
