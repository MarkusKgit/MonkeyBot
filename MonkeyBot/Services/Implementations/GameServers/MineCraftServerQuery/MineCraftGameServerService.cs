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
    public class MineCraftGameServerService : BaseGameServerService
    {        
        private readonly MonkeyDBContext dbContext;
        private readonly DiscordSocketClient discordClient;
        private readonly ILogger<MineCraftGameServerService> logger;

        public MineCraftGameServerService(
            MonkeyDBContext dbContext,            
            DiscordSocketClient discordClient,
            ILogger<MineCraftGameServerService> logger)
            : base(GameServerType.Minecraft, dbContext, discordClient, logger)
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
            MineQuery query = null;
            try
            {
                query = new MineQuery(discordGameServer.ServerIP.Address, discordGameServer.ServerIP.Port, logger);
                MineQueryResult serverInfo = await query.GetServerInfoAsync().ConfigureAwait(false);
                if (serverInfo == null)
                {
                    return false;
                }
                SocketGuild guild = discordClient?.GetGuild(discordGameServer.GuildID);
                SocketTextChannel channel = guild?.GetTextChannel(discordGameServer.ChannelID);
                if (guild == null || channel == null)
                {
                    return false;
                }
                EmbedBuilder builder = new DiscordEmbedBuilder()
                    .WithColor(new Color(21, 26, 35))
                    .WithTitle($"Minecraft Server ({discordGameServer.ServerIP.Address}:{discordGameServer.ServerIP.Port})")
                    .WithDescription($"Motd: {serverInfo.Description.Motd}");

                _ = serverInfo.Players.Sample != null && serverInfo.Players.Sample.Count > 0
                    ? builder.AddField($"Online Players ({serverInfo.Players.Online}/{serverInfo.Players.Max})", string.Join(", ", serverInfo.Players.Sample.Select(x => x.Name)))
                    : builder.AddField("Online Players", $"{serverInfo.Players.Online}/{serverInfo.Players.Max}");

                if (discordGameServer.GameVersion.IsEmpty())
                {
                    discordGameServer.GameVersion = serverInfo.Version.Name;
                    _ = dbContext.GameServers.Update(discordGameServer);
                    _ = await dbContext.SaveChangesAsync().ConfigureAwait(false);
                }
                else
                {
                    if (serverInfo.Version.Name != discordGameServer.GameVersion)
                    {
                        discordGameServer.GameVersion = serverInfo.Version.Name;
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

                _ = builder.WithFooter($"Server version: {serverInfo.Version.Name}{lastServerUpdate} || Last check: {DateTime.Now}");

                // Generate chart every full 10 minutes                
                if (DateTime.Now.Minute % 10 == 0)
                {
                    string chart = await GenerateHistoryChartAsync(discordGameServer, serverInfo.Players.Online, serverInfo.Players.Max).ConfigureAwait(false);

                    if (!chart.IsEmptyOrWhiteSpace())
                    {
                        _ = builder.AddField("Player Count History", chart);
                    }
                }

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
                    IUserMessage message = await (channel?.SendMessageAsync("", false, builder.Build())).ConfigureAwait(false);
                    discordGameServer.MessageID = message.Id;
                    _ = dbContext.GameServers.Update(discordGameServer);
                    _ = await dbContext.SaveChangesAsync().ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, $"Error getting updates for server {discordGameServer.ServerIP}");
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