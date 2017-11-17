using Discord;
using Discord.WebSocket;
using FluentScheduler;
using Microsoft.Extensions.DependencyInjection;
using MonkeyBot.Services.Common.SteamServerQuery;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace MonkeyBot.Services
{
    public class GameServerService : IGameServerService
    {
        private DbService db;
        private DiscordSocketClient client;

        //private List<DiscordGameServerInfo> servers = new List<DiscordGameServerInfo>();

        public GameServerService(IServiceProvider provider)
        {
            db = provider.GetService<DbService>();
            client = provider.GetService<DiscordSocketClient>();
        }

        public void Initialize()
        {
            JobManager.AddJob(async () => await PostAllServerInfoAsync(), (x) => x.ToRunNow().AndEvery(1).Minutes());
        }

        public async Task AddServerAsync(IPEndPoint endpoint, ulong guildID, ulong channelID)
        {
            var server = new DiscordGameServerInfo(endpoint, guildID, channelID);
            //servers.Add(server);
            using (var uow = db.UnitOfWork)
            {
                await uow.GameServers.AddOrUpdateAsync(server);
                await uow.CompleteAsync();
            }
            await PostServerInfoAsync(server);
        }

        private async Task PostAllServerInfoAsync()
        {
            var servers = await GetServersAsync();
            foreach (var server in servers)
            {
                await PostServerInfoAsync(server);
            }
        }

        private async Task PostServerInfoAsync(DiscordGameServerInfo discordGameServer)
        {
            try
            {
                var server = new SteamGameServer(discordGameServer.IP);
                var serverInfo = await server.GetServerInfoAsync();
                var playerInfo = await server.GetPlayersAsync();
                var guild = client.GetGuild(discordGameServer.GuildId);
                var channel = guild?.GetTextChannel(discordGameServer.ChannelId);
                if (guild == null || channel == null)
                    return;
                var builder = new EmbedBuilder();
                builder.WithColor(new Color(21, 26, 35));
                builder.WithTitle("Server Update");
                builder.WithDescription(serverInfo.Name);
                builder.AddField("Online Players", $"{serverInfo.Players}/{serverInfo.MaxPlayers}");
                builder.AddField("Current Map", serverInfo.Map);
                builder.WithFooter($"Last update: {DateTime.Now}");
                if (discordGameServer.Message == null)
                    discordGameServer.Message = await channel?.SendMessageAsync("", false, builder.Build());
                else
                    await discordGameServer.Message.ModifyAsync(x => x.Embed = builder.Build());
            }
            catch
            {
                Console.WriteLine($"{DateTime.Now} Error getting updates for server {discordGameServer.IP}");
                throw;
            }
        }

        public async Task RemoveServerAsync(IPEndPoint endPoint, ulong guildID)
        {
            var servers = await GetServersAsync();
            var serverToRemove = servers.Where(x => x.IP.Address == endPoint.Address && x.IP.Port == endPoint.Port && x.GuildId == guildID).FirstOrDefault();
            if (serverToRemove == null)
                throw new ArgumentException("The specified server does not exist");
            if (serverToRemove.Message != null)
                await serverToRemove.Message.DeleteAsync();
            //servers.Remove(serverToRemove);
            using (var uow = db.UnitOfWork)
            {
                await uow.GameServers.RemoveAsync(serverToRemove);
                await uow.CompleteAsync();
            }
        }

        private async Task<List<DiscordGameServerInfo>> GetServersAsync()
        {
            using (var uow = db.UnitOfWork)
            {
                return await uow.GameServers.GetAllAsync();
            }
        }
    }
}