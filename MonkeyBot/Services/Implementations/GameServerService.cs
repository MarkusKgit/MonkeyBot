using Discord;
using Discord.WebSocket;
using FluentScheduler;
using Microsoft.Extensions.DependencyInjection;
using MonkeyBot.Services.Common.SteamServerQuery;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace MonkeyBot.Services
{
    public class GameServerService : IGameServerService
    {
        private DbService db;
        private DiscordSocketClient client;

        private List<DiscordGameServerInfo> servers = new List<DiscordGameServerInfo>();

        public GameServerService(IServiceProvider provider)
        {
            db = provider.GetService<DbService>();
            client = provider.GetService<DiscordSocketClient>();
            JobManager.AddJob(async () => await PostAllServerInfoAsync(), (x) => x.ToRunEvery(15).Seconds());
        }

        public async Task AddServerAsync(IPEndPoint endpoint, ulong guildID, ulong channelID)
        {
            var server = new DiscordGameServerInfo(endpoint, guildID, channelID);
            servers.Add(server);
            await PostServerInfoAsync(server);
        }

        private async Task PostAllServerInfoAsync()
        {
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
                string connectLink = $"steam://rungameid/{serverInfo.GameId}//%20+connect%20{discordGameServer.IP.Address}:{serverInfo.Port}";

                builder.AddField("Connect", $"[Click to connect]({connectLink})");
                if (discordGameServer.Message == null)
                    discordGameServer.Message = await channel?.SendMessageAsync("", false, builder.Build());
                else
                    await discordGameServer.Message.ModifyAsync(x => x.Embed = builder.Build());
            }
            catch
            {
                Console.WriteLine($"Error getting updates for server {discordGameServer.IP}");
            }
        }

        public void RemoveServer(IPEndPoint endPoint, ulong guildID)
        {
            throw new NotImplementedException();
        }
    }
}