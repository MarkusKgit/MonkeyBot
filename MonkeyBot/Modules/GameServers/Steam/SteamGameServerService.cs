using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;
using MonkeyBot.Database;
using MonkeyBot.Models;
using MonkeyBot.Services.Implementations.GameServers.SteamServerQuery;
using SteamQueryNet;
using SteamQueryNet.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MonkeyBot.Services
{
    public class SteamGameServerService : BaseGameServerService
    {
        private readonly MonkeyDBContext dbContext;
        private readonly DiscordClient discordClient;
        private readonly ILogger<SteamGameServerService> logger;

        public SteamGameServerService(MonkeyDBContext dbContext, DiscordClient discordClient, ILogger<SteamGameServerService> logger)
            : base(GameServerType.Steam, dbContext, discordClient, logger)
        {
            this.dbContext = dbContext;
            this.discordClient = discordClient;
            this.logger = logger;
        }

        protected override async Task<bool> PostServerInfoAsync(GameServer discordGameServer)
        {
            if (discordGameServer == null)
            {
                return false;
            }

            ServerQuery serverQuery = null;

            try
            {
                using var udpClient = new UdpWrapper();
                serverQuery = new ServerQuery(udpClient, null);
                serverQuery.Connect(discordGameServer.ServerIP.ToString());
                ServerInfo serverInfo = await serverQuery.GetServerInfoAsync();
                List<Player> players = (await serverQuery.GetPlayersAsync()).Where(p => !p.Name.IsEmptyOrWhiteSpace()).ToList();
                if (serverInfo == null || players == null)
                {
                    return false;
                }
                if (!discordClient.Guilds.TryGetValue(discordGameServer.GuildID, out DiscordGuild guild))
                {
                    return false;
                }
                DiscordChannel channel = guild?.GetChannel(discordGameServer.ChannelID);
                if (guild == null || channel == null)
                {
                    return false;
                }

                string onlinePlayers = players.Count > serverInfo.MaxPlayers
                    ? $"{serverInfo.MaxPlayers}(+{players.Count - serverInfo.MaxPlayers})/{serverInfo.MaxPlayers}"
                    : $"{players.Count}/{serverInfo.MaxPlayers}";

                var builder = new DiscordEmbedBuilder()
                    .WithColor(new DiscordColor(21, 26, 35))
                    .WithTitle($"{serverInfo.Game} Server ({discordGameServer.ServerIP.Address}:{serverInfo.Port})")
                    .WithDescription(serverInfo.Name)
                    .AddField("Online Players", onlinePlayers)
                    .AddField("Current Map", serverInfo.Map);

                if (players != null && players.Count > 0)
                {
                    _ = builder.AddField("Currently connected players:", string.Join(", ", players.Select(x => x.Name).Where(name => !name.IsEmpty()).OrderBy(x => x)).TruncateTo(1023));
                }

                //Discord removed support for protocols other than http or https so this currently makes no sense. Leaving it here, in case they re-enable it
                //string connectLink = $"steam://connect/{discordGameServer.ServerIP.Address}:{serverInfo.Port}";
                //_ = builder.AddField("Connect using this link", connectLink);

                if (discordGameServer.GameVersion.IsEmpty())
                {
                    discordGameServer.GameVersion = serverInfo.Version;
                    _ = dbContext.GameServers.Update(discordGameServer);
                    _ = await dbContext.SaveChangesAsync();
                }
                else
                {
                    if (serverInfo.Version != discordGameServer.GameVersion)
                    {
                        discordGameServer.GameVersion = serverInfo.Version;
                        discordGameServer.LastVersionUpdate = DateTime.Now;
                        _ = dbContext.GameServers.Update(discordGameServer);
                        _ = await dbContext.SaveChangesAsync();
                    }
                }


                string lastServerUpdate = "";
                if (discordGameServer.LastVersionUpdate.HasValue)
                {
                    lastServerUpdate = $" (Last update: {discordGameServer.LastVersionUpdate.Value})";
                }

                _ = builder.AddField("Server version", $"{serverInfo.Version}{lastServerUpdate}");
                _ = builder.WithFooter($"Last check: {DateTime.Now}");

                string chart = await GenerateHistoryChartAsync(discordGameServer, serverInfo.Players, serverInfo.MaxPlayers);
                if (!chart.IsEmptyOrWhiteSpace())
                {
                    _ = builder.AddField("Player Count History", chart);
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
                        logger.LogWarning($"Error getting updates for server {discordGameServer.ServerIP}. Original message was removed.");
                        await RemoveServerAsync(discordGameServer.ServerIP, discordGameServer.GuildID);
                        _ = await channel.SendMessageAsync($"Error getting updates for server {discordGameServer.ServerIP}. Original message was removed. Please use the proper remove command to remove the gameserver");
                        return false;
                    }
                }
                else
                {
                    discordGameServer.MessageID = (await (channel?.SendMessageAsync(builder.Build()))).Id;
                    _ = dbContext.GameServers.Update(discordGameServer);
                    _ = await dbContext.SaveChangesAsync();
                }

            }
            catch (TimeoutException tex)
            {
                logger.LogInformation(tex, $"Timed out trying to get steam server info for {discordGameServer.GameServerType} server {discordGameServer.ServerIP}");
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, $"Error getting updates for {discordGameServer.GameServerType} server {discordGameServer.ServerIP}");
                throw;
            }
            finally
            {
                if (serverQuery != null)
                {
                    serverQuery.Dispose();
                }
            }
            return true;
        }


    }
}