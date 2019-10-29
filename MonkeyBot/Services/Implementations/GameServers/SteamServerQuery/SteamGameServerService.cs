using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using MonkeyBot.Database;
using MonkeyBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MonkeyBot.Services
{
    public class SteamGameServerService : BaseGameServerService
    {
        private readonly MonkeyDBContext dbContext;
        private readonly DiscordSocketClient discordClient;
        private readonly ILogger<SteamGameServerService> logger;

        public SteamGameServerService(MonkeyDBContext dbContext, DiscordSocketClient discordClient, ILogger<SteamGameServerService> logger)
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

            SteamGameServer server = null;
            try
            {
                server = new SteamGameServer(discordGameServer.ServerIP);
                SteamServerInfo serverInfo = await (server?.GetServerInfoAsync()).ConfigureAwait(false);
                List<PlayerInfo> playerInfo = (await (server?.GetPlayersAsync()).ConfigureAwait(false)).Where(x => !x.Name.IsEmpty()).ToList();
                if (serverInfo == null || playerInfo == null)
                {
                    return false;
                }
                SocketGuild guild = discordClient?.GetGuild(discordGameServer.GuildID);
                ITextChannel channel = guild?.GetTextChannel(discordGameServer.ChannelID);
                if (guild == null || channel == null)
                {
                    return false;
                }
                var builder = new EmbedBuilder()
                    .WithColor(new Color(21, 26, 35))
                    .WithTitle($"{serverInfo.Description} Server ({discordGameServer.ServerIP.Address}:{serverInfo.Port})")
                    .WithDescription(serverInfo.Name)
                    .AddField("Online Players", $"{playerInfo.Count}/{serverInfo.MaxPlayers}")
                    .AddField("Current Map", serverInfo.Map);
                if (playerInfo != null && playerInfo.Count > 0)
                {
                    _ = builder.AddField("Currently connected players:", string.Join(", ", playerInfo.Select(x => x.Name).Where(name => !name.IsEmpty()).OrderBy(x => x)).TruncateTo(1023));
                }

                string connectLink = $"steam://rungameid/{serverInfo.GameId}//%20+connect%20{discordGameServer.ServerIP.Address}:{serverInfo.Port}";
                _ = builder.AddField("Connect using this link", $"[{connectLink}]({connectLink})");

                if (discordGameServer.GameVersion.IsEmpty())
                {
                    discordGameServer.GameVersion = serverInfo.GameVersion;
                    _ = dbContext.GameServers.Update(discordGameServer);
                    _ = await dbContext.SaveChangesAsync().ConfigureAwait(false);
                }
                else
                {
                    if (serverInfo.GameVersion != discordGameServer.GameVersion)
                    {
                        discordGameServer.GameVersion = serverInfo.GameVersion;
                        discordGameServer.LastVersionUpdate = DateTime.Now;
                        _ = dbContext.GameServers.Update(discordGameServer);
                        _ = await dbContext.SaveChangesAsync().ConfigureAwait(false);
                    }
                }


                string lastServerUpdate = "";
                if (discordGameServer.LastVersionUpdate.HasValue)
                {
                    lastServerUpdate = $" (Last update: {discordGameServer.LastVersionUpdate.Value})";
                }

                _ = builder.AddField("Server version", $"{serverInfo.GameVersion}{lastServerUpdate}");
                _ = builder.WithFooter($"Last check: {DateTime.Now}");

                if (discordGameServer.MessageID.HasValue)
                {
                    if (await channel.GetMessageAsync(discordGameServer.MessageID.Value).ConfigureAwait(false) is IUserMessage existingMessage && existingMessage != null)
                    {
                        await existingMessage.ModifyAsync(x => x.Embed = builder.Build()).ConfigureAwait(false);
                    }
                    else
                    {
                        logger.LogWarning($"Error getting updates for server {discordGameServer.ServerIP}. Original message was removed.");
                        await RemoveServerAsync(discordGameServer.ServerIP, discordGameServer.GuildID).ConfigureAwait(false);
                        _ = await channel.SendMessageAsync($"Error getting updates for server {discordGameServer.ServerIP}. Original message was removed. Please use the proper remove command to remove the gameserver").ConfigureAwait(false);
                        return false;
                    }
                }
                else
                {
                    discordGameServer.MessageID = (await (channel?.SendMessageAsync("", false, builder.Build())).ConfigureAwait(false)).Id;
                    _ = dbContext.GameServers.Update(discordGameServer);
                    _ = await dbContext.SaveChangesAsync().ConfigureAwait(false);
                }
            }
            catch (TimeoutException tex)
            {
                logger.LogWarning(tex, "Timed out trying to get steam server info");
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, $"Error getting updates for server {discordGameServer.ServerIP}");
                throw;
            }
            finally
            {
                if (server != null)
                {
                    server.Dispose();
                }
            }
            return true;
        }
    }
}