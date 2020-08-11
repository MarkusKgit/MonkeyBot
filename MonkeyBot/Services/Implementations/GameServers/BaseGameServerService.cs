using Discord;
using Discord.WebSocket;
using FluentScheduler;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MonkeyBot.Common;
using MonkeyBot.Database;
using MonkeyBot.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
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
            List<GameServer> servers = await dbContext.GameServers.AsQueryable().Where(x => x.GameServerType == gameServerType).ToListAsync().ConfigureAwait(false);
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

        public async Task<List<GameServer>> ListServers(ulong guildID)
            => await dbContext.GameServers.AsQueryable().Where(g => g.GuildID == guildID).ToListAsync().ConfigureAwait(false);

        public async Task RemoveServerAsync(IPEndPoint endPoint, ulong guildID)
        {
            GameServer serverToRemove = (await dbContext.GameServers.AsQueryable().ToListAsync().ConfigureAwait(false)).FirstOrDefault(x => x.ServerIP.Address.ToString() == endPoint.Address.ToString() && x.ServerIP.Port == endPoint.Port && x.GuildID == guildID);
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

        private static readonly TimeSpan OneMinute = TimeSpan.FromMinutes(1);

        protected static async Task<string> GenerateHistoryChartAsync(GameServer discordGameServer, int currentPlayers, int maxPlayers)
        {

            string id = discordGameServer.ServerIP.ToString().Replace(".", "_").Replace(":", "_");

            var historyPeriod = TimeSpan.FromMinutes(90);
            const string folder = "Gameservers";

            if (!Directory.Exists(folder))
            {
                _ = Directory.CreateDirectory(folder);
            }

            string baseFilePath = Path.Combine(folder, id);
            string storedValuesPath = $"{baseFilePath}.txt";

            DateTime now = DateTime.Now;
            DateTime minTime = now.Subtract(historyPeriod);

            var historicData = new List<HistoricData<int>>();
            if (File.Exists(storedValuesPath))
            {
                string json = await MonkeyHelpers.ReadTextAsync(storedValuesPath).ConfigureAwait(false);
                List<HistoricData<int>> loadedData = JsonSerializer.Deserialize<List<HistoricData<int>>>(json);
                historicData = loadedData
                    .Where(x => x.Time > minTime)
                    .ToList();
            }

            historicData.Add(new HistoricData<int>(now, Math.Min(currentPlayers, maxPlayers)));

            await MonkeyHelpers.WriteTextAsync(storedValuesPath, JsonSerializer.Serialize(historicData, new JsonSerializerOptions() { WriteIndented = true }))
                .ConfigureAwait(false);

            int maxIntervals = 10;
            int interval = 10;

            for (int i = (int)Math.Ceiling(1.0 * maxPlayers / maxIntervals); i < 10; i++)
            {
                if (maxPlayers % i == 0)
                {
                    interval = i;
                    break;
                }
            }

            List<int> roundedPlayerCounts = Enumerable
                .Range(0, 10)
                .Reverse()
                .Select(mins => now.Subtract(TimeSpan.FromMinutes(mins * 10))) // Last 90 minutes
                .Select(t => historicData.FirstOrDefault(hd => Math.Abs(hd.Time.Subtract(t).TotalMinutes) < 1)?.Value ?? 0)
                .Select(v => (int)Math.Round(v / (1.0 * interval)))
                .ToList();

            //Bottom up
            var lines = new List<string>();
            lines.Add("      minutes ago");
            lines.Add("   0┊0┊0┊0┊0┊0┊0┊0┊0┊0");
            lines.Add("   9┊8┊7┊6┊5┊4┊3┊2┊1┊0");

            int maxI = maxPlayers / interval;

            for (int i = 0; i <= maxI; i++)
            {
                string line = $"{i * interval, 2}";
                line += i == 0 ? "┴" : i == maxI ? "┐" : "┤";
                string joinChar = i == 0 ? "┼" : i == maxI ? "┬" : "┊";
                line += string.Join(joinChar, 
                    Enumerable
                    .Range(0, 10)
                    .Select(n => roundedPlayerCounts[n])
                    .Select(cnt => (i, cnt) switch
                    {
                        (0,0) => "─",
                        (_,0) => " ",
                        (0,_) => "╨",
                        var (ii, c) when ii < c =>  "║",
                        var (ii, c) when ii == c => "╥",
                        _ => " "
                    })
                    );
                line += i == 0 ? "┘" : i == maxI ? "┐" : "│";
                lines.Add(line);
            }            
            lines.Reverse();
            string table = $"```{string.Join(Environment.NewLine, lines)} ```";

            return table;
        }

        const string table1 = @"```
80┐ ┬ ┬ ┬ ┬ ┬ ┬ ┬ ┬ ┬ ┐
70┤ ┊ ┊ ┊ ┊ ┊ ┊ ┊ ┊ │╥│
60┤ ┊ ┊ ┊ ┊ ┊ ┊ ┊ ┊╥│║│
50┤ ┊ ┊ ┊ ┊ ┊ ┊ ┊╥┊║│║│
40┤ ┊ ┊ ┊ ┊ ┊ ┊╥┊║┊║│║│
30┤ ┊ ┊ ┊ ┊ ┊╥┊║┊║┊║│║│
20┤ ┊ ┊ ┊ ┊╥┊║┊║┊║┊║│║│
10┤ ┊ ┊ ┊╥┊║┊║┊║┊║┊║│║│
 0┴─┼─┼─┼╨┼╨┼╨┼╨┼╨┼╨┼╨┘
   9┊8┊7┊6┊5┊4┊3┊2┊1┊0  
   0┊0┊0┊0┊0┊0┊0┊0┊0┊0 
      minutes ago```";
    }
}