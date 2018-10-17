using Discord.WebSocket;
using FluentScheduler;
using Microsoft.Extensions.Logging;
using MonkeyBot.Services.Common;
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
        protected readonly DbService dbService;
        protected readonly DiscordSocketClient discordClient;
        protected readonly ILogger<IGameServerService> logger;

        protected BaseGameServerService(GameServerType gameServerType, DbService dbService, DiscordSocketClient discordClient, ILogger<IGameServerService> logger)
        {
            this.gameServerType = gameServerType;
            this.dbService = dbService;
            this.discordClient = discordClient;
            this.logger = logger;
        }

        public void Initialize()
        {
            JobManager.AddJob(async () => await PostAllServerInfoAsync(), (x) => x.ToRunNow().AndEvery(1).Minutes());
        }

        public async Task AddServerAsync(IPEndPoint endpoint, ulong guildID, ulong channelID)
        {
            var server = new DiscordGameServerInfo(gameServerType, endpoint, guildID, channelID);
            using (var uow = dbService.UnitOfWork)
            {
                await uow.GameServers.AddOrUpdateAsync(server);
                await uow.CompleteAsync();
            }
            await PostServerInfoAsync(server);
        }

        protected abstract Task PostServerInfoAsync(DiscordGameServerInfo discordGameServer);

        private async Task PostAllServerInfoAsync()
        {
            var servers = (await GetServersAsync()).Where(x => x.GameServerType == gameServerType);
            foreach (var server in servers)
            {
                try
                {
                    await PostServerInfoAsync(server);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Error posting server infos");
                }
            }
        }

        public async Task RemoveServerAsync(IPEndPoint endPoint, ulong guildID)
        {
            var servers = await GetServersAsync();
            var serverToRemove = servers.FirstOrDefault(x => x.IP.Address.ToString() == endPoint.Address.ToString() && x.IP.Port == endPoint.Port && x.GuildId == guildID);
            if (serverToRemove == null)
                throw new ArgumentException("The specified server does not exist");
            if (serverToRemove.MessageId != null)
            {
                var guild = discordClient.GetGuild(serverToRemove.GuildId);
                var channel = guild?.GetTextChannel(serverToRemove.ChannelId);
                var msg = await channel?.GetMessageAsync(serverToRemove.MessageId.Value) as Discord.Rest.RestUserMessage;
                await msg?.DeleteAsync();
            }

            using (var uow = dbService.UnitOfWork)
            {
                await uow.GameServers.RemoveAsync(serverToRemove);
                await uow.CompleteAsync();
            }
        }

        private async Task<List<DiscordGameServerInfo>> GetServersAsync()
        {
            using (var uow = dbService.UnitOfWork)
            {
                return await uow.GameServers.GetAllAsync();
            }
        }
    }
}