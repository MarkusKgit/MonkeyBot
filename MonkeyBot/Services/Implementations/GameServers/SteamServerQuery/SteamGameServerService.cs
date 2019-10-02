using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using MonkeyBot.Database;
using MonkeyBot.Models;
using System;
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
                return false;
            SteamGameServer server = null;
            try
            {
                server = new SteamGameServer(discordGameServer.ServerIP);
                var serverInfo = await (server?.GetServerInfoAsync()).ConfigureAwait(false);
                var playerInfo = (await (server?.GetPlayersAsync()).ConfigureAwait(false)).Where(x => !x.Name.IsEmpty()).ToList();
                if (serverInfo == null || playerInfo == null)
                    return false;
                var guild = discordClient?.GetGuild(discordGameServer.GuildID);
                var channel = guild?.GetTextChannel(discordGameServer.ChannelID);
                if (guild == null || channel == null)
                    return false;
                var builder = new EmbedBuilder();
                builder.WithColor(new Color(21, 26, 35));
                builder.WithTitle($"{serverInfo.Description} Server ({discordGameServer.ServerIP.Address}:{serverInfo.Port})");
                builder.WithDescription(serverInfo.Name);
                builder.AddField("Online Players", $"{playerInfo.Count}/{serverInfo.MaxPlayers}");
                builder.AddField("Current Map", serverInfo.Map);
                if (playerInfo != null && playerInfo.Count > 0)
                    builder.AddField("Currently connected players:", string.Join(", ", playerInfo.Select(x => x.Name).Where(name => !name.IsEmpty()).OrderBy(x => x)));
                string connectLink = $"steam://rungameid/{serverInfo.GameId}//%20+connect%20{discordGameServer.ServerIP.Address}:{serverInfo.Port}";
                builder.AddField("Connect", $"[Click to connect]({connectLink})");

                if (discordGameServer.GameVersion.IsEmpty())
                {
                    discordGameServer.GameVersion = serverInfo.GameVersion;
                    dbContext.GameServers.Update(discordGameServer);
                    await dbContext.SaveChangesAsync().ConfigureAwait(false);
                }
                else
                {
                    if (serverInfo.GameVersion != discordGameServer.GameVersion)
                    {
                        discordGameServer.GameVersion = serverInfo.GameVersion;
                        discordGameServer.LastVersionUpdate = DateTime.Now;
                        dbContext.GameServers.Update(discordGameServer);
                        await dbContext.SaveChangesAsync().ConfigureAwait(false);
                    }
                }


                string lastServerUpdate = "";
                if (discordGameServer.LastVersionUpdate.HasValue)
                    lastServerUpdate = $" (Last update: {discordGameServer.LastVersionUpdate.Value})";
                builder.AddField("Server version", $"{serverInfo.GameVersion}{lastServerUpdate}");

                builder.WithFooter($"Last check: {DateTime.Now}");
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
                        await channel.SendMessageAsync($"Error getting updates for server {discordGameServer.ServerIP}. Original message was removed. Please use the proper remove command to remove the gameserver").ConfigureAwait(false);
                        return false;
                    }
                }
                else
                {
                    discordGameServer.MessageID = (await (channel?.SendMessageAsync("", false, builder.Build())).ConfigureAwait(false)).Id;
                    dbContext.GameServers.Update(discordGameServer);
                    await dbContext.SaveChangesAsync().ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, $"Error getting updates for server {discordGameServer.ServerIP}");
                throw;
            }
            finally
            {
                if (server != null)
                    server.Dispose();
            }
            return true;
        }
    }
}