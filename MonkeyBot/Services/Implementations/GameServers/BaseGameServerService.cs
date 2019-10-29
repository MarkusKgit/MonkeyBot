using Discord;
using Discord.WebSocket;
using FluentScheduler;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MonkeyBot.Database;
using MonkeyBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace MonkeyBot.Services
{
    public abstract class BaseGameServerService : IGameServerService
    {
        private readonly GameServerType gameServerType;
        private readonly MonkeyDBContext dbContext;
        private readonly DiscordSocketClient discordClient;
        private readonly ILogger<IGameServerService> logger;

        protected BaseGameServerService(GameServerType gameServerType, MonkeyDBContext dbContext, DiscordSocketClient discordClient, ILogger<IGameServerService> logger)
        {
            this.gameServerType = gameServerType;
            this.dbContext = dbContext;
            this.discordClient = discordClient;
            this.logger = logger;
        }

        public void Initialize()
            => JobManager.AddJob(async () => await PostAllServerInfoAsync().ConfigureAwait(false), (x) => x.ToRunNow().AndEvery(1).Minutes());

        public async Task<bool> AddServerAsync(IPEndPoint endpoint, ulong guildID, ulong channelID)
        {
            var server = new GameServer { GameServerType = gameServerType, ServerIP = endpoint, GuildID = guildID, ChannelID = channelID };
            bool success = await PostServerInfoAsync(server).ConfigureAwait(false);
            if (success && !dbContext.GameServers.Contains(server))
            {
                _ = dbContext.Add(server);
                _ = await dbContext.SaveChangesAsync().ConfigureAwait(false);
            }
            return success;
        }

        protected abstract Task<bool> PostServerInfoAsync(GameServer discordGameServer);

        private async Task PostAllServerInfoAsync()
        {
            List<GameServer> servers = await dbContext.GameServers.Where(x => x.GameServerType == gameServerType).ToListAsync().ConfigureAwait(false);
            foreach (GameServer server in servers)
            {
                try
                {
                    _ = await PostServerInfoAsync(server).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Error posting server infos");
                }
            }
        }

        public async Task RemoveServerAsync(IPEndPoint endPoint, ulong guildID)
        {
            GameServer serverToRemove = (await dbContext.GameServers.ToListAsync().ConfigureAwait(false)).FirstOrDefault(x => x.ServerIP.Address.ToString() == endPoint.Address.ToString() && x.ServerIP.Port == endPoint.Port && x.GuildID == guildID);
            if (serverToRemove == null)
            {
                throw new ArgumentException("The specified server does not exist");
            }
            if (serverToRemove.MessageID != null)
            {
                try
                {
                    SocketGuild guild = discordClient.GetGuild(serverToRemove.GuildID);
                    ITextChannel channel = guild?.GetTextChannel(serverToRemove.ChannelID);
                    if (await (channel?.GetMessageAsync(serverToRemove.MessageID.Value)).ConfigureAwait(false) is Discord.Rest.RestUserMessage msg)
                    {
                        await msg.DeleteAsync().ConfigureAwait(false);
                    }
                }
                catch (Exception e)
                {
                    logger.LogError(e, $"Error trying to remove message for game server {endPoint.Address}");
                }
            }
            _ = dbContext.GameServers.Remove(serverToRemove);
            _ = await dbContext.SaveChangesAsync().ConfigureAwait(false);
        }
    }
}