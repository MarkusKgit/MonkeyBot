using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MonkeyBot.Database;
using MonkeyBot.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MonkeyBot.Services
{
    public class MineCraftGameServerService : BaseGameServerService
    {        
        private readonly IDbContextFactory<MonkeyDBContext> _dbContextFactory;
        private readonly DiscordClient _discordClient;        
        private readonly ILogger<MineCraftGameServerService> _logger;

        public MineCraftGameServerService(
            IDbContextFactory<MonkeyDBContext> dbContextFactory,            
            DiscordClient discordClient,
            ISchedulingService schedulingService,
            ILogger<MineCraftGameServerService> logger)
            : base(GameServerType.Minecraft, dbContextFactory, discordClient, schedulingService, logger)
        {
            _dbContextFactory = dbContextFactory;
            _discordClient = discordClient;
            _logger = logger;
        }

        protected override async Task<bool> PostServerInfoAsync(GameServer discordGameServer)
        {
            if (discordGameServer == null)
            {
                return false;
            }
            MineQuery query = null;
            try
            {
                query = new MineQuery(discordGameServer.ServerIP.Address, discordGameServer.ServerIP.Port, _logger);
                MineQueryResult serverInfo = await query.GetServerInfoAsync();
                if (serverInfo == null)
                {
                    return false;
                }
                if (!_discordClient.Guilds.TryGetValue(discordGameServer.GuildID, out DiscordGuild guild))
                {
                    return false;
                }
                DiscordChannel channel = guild?.GetChannel(discordGameServer.ChannelID);
                if (guild == null || channel == null)
                {
                    return false;
                }
                DiscordEmbedBuilder builder = new DiscordEmbedBuilder()
                    .WithColor(new DiscordColor(21, 26, 35))
                    .WithTitle($"Minecraft Server ({discordGameServer.ServerIP.Address}:{discordGameServer.ServerIP.Port})")
                    .WithDescription($"Motd: {serverInfo.Description.Motd}");

                _ = serverInfo.Players.Sample != null && serverInfo.Players.Sample.Count > 0
                    ? builder.AddField($"Online Players ({serverInfo.Players.Online}/{serverInfo.Players.Max})", string.Join(", ", serverInfo.Players.Sample.Select(x => x.Name)))
                    : builder.AddField("Online Players", $"{serverInfo.Players.Online}/{serverInfo.Players.Max}");

                using var dbContext = _dbContextFactory.CreateDbContext();
                if (discordGameServer.GameVersion.IsEmpty())
                {
                    discordGameServer.GameVersion = serverInfo.Version.Name;
                    dbContext.GameServers.Update(discordGameServer);
                    await dbContext.SaveChangesAsync();
                }
                else
                {
                    if (serverInfo.Version.Name != discordGameServer.GameVersion)
                    {
                        discordGameServer.GameVersion = serverInfo.Version.Name;
                        discordGameServer.LastVersionUpdate = DateTime.Now;
                        dbContext.GameServers.Update(discordGameServer);
                        await dbContext.SaveChangesAsync();
                    }
                }
                string lastServerUpdate = "";
                if (discordGameServer.LastVersionUpdate.HasValue)
                {
                    lastServerUpdate = $" (Last update: {discordGameServer.LastVersionUpdate.Value})";
                }

                builder.WithFooter($"Server version: {serverInfo.Version.Name}{lastServerUpdate} || Last check: {DateTime.Now}");

                // Generate chart every full 10 minutes                
                if (DateTime.Now.Minute % 10 == 0)
                {
                    string chart = await GenerateHistoryChartAsync(discordGameServer, serverInfo.Players.Online, serverInfo.Players.Max);

                    if (!chart.IsEmptyOrWhiteSpace())
                    {
                        builder.AddField("Player Count History", chart);
                    }
                }

                if (discordGameServer.MessageID.HasValue)
                {
                    DiscordMessage existingMessage = await channel.GetMessageAsync(discordGameServer.MessageID.Value);
                    if (existingMessage != null)
                    {
                        await existingMessage.ModifyAsync(embed: builder.Build());
                    }
                    else
                    {
                        _logger.LogWarning($"Error getting updates for server {discordGameServer.ServerIP}. Original message was removed.");
                        await RemoveServerAsync(discordGameServer.ServerIP, discordGameServer.GuildID);
                        await channel.SendMessageAsync($"Error getting updates for server {discordGameServer.ServerIP}. Original message was removed. Please use the proper remove command to remove the gameserver");
                        return false;
                    }
                }
                else
                {
                    DiscordMessage message = await (channel?.SendMessageAsync(builder.Build()));
                    discordGameServer.MessageID = message.Id;
                    dbContext.GameServers.Update(discordGameServer);
                    await dbContext.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Error getting updates for server {discordGameServer.ServerIP}");
                throw;
            }
            finally
            {
                if (query != null)
                {
                    query.Dispose();
                }
            }
            return true;
        }
    }
}