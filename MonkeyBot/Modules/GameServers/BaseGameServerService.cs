using DSharpPlus;
using DSharpPlus.Entities;
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
using System.Text.Json;
using System.Threading.Tasks;

namespace MonkeyBot.Services
{
    public abstract class BaseGameServerService : IGameServerService
    {
        private static readonly TimeSpan _updateIntervall = TimeSpan.FromMinutes(1);
        private static readonly TimeSpan _startDelay = TimeSpan.FromSeconds(10);

        private readonly GameServerType _gameServerType;
        private readonly MonkeyDBContext _dbContext;
        private readonly DiscordClient _discordClient;
        private readonly ISchedulingService _schedulingService;
        private readonly ILogger<IGameServerService> _logger;

        protected BaseGameServerService(GameServerType gameServerType, MonkeyDBContext dbContext, DiscordClient discordClient, ISchedulingService schedulingService, ILogger<IGameServerService> logger)
        {
            _gameServerType = gameServerType;
            _dbContext = dbContext;
            _discordClient = discordClient;
            _schedulingService = schedulingService;
            _logger = logger;
        }

        public void Initialize()
            => _schedulingService.ScheduleJobRecurring("GameServerInfos", _updateIntervall, PostAllServerInfoAsync(), _startDelay);

        public async Task<bool> AddServerAsync(IPEndPoint endpoint, ulong guildID, ulong channelID)
        {
            var server = new GameServer { GameServerType = _gameServerType, ServerIP = endpoint, GuildID = guildID, ChannelID = channelID };
            bool success = await PostServerInfoAsync(server);
            if (success && !_dbContext.GameServers.Contains(server))
            {
                _ = _dbContext.Add(server);
                _ = await _dbContext.SaveChangesAsync();
            }
            return success;
        }

        protected abstract Task<bool> PostServerInfoAsync(GameServer discordGameServer);

        private async Task PostAllServerInfoAsync()
        {
            List<GameServer> servers = await _dbContext.GameServers.AsQueryable().Where(x => x.GameServerType == _gameServerType).ToListAsync();
            foreach (GameServer server in servers)
            {
                try
                {
                    _ = await PostServerInfoAsync(server);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error posting server infos");
                }
            }
        }

        public async Task<List<GameServer>> ListServers(ulong guildID)
            => await _dbContext.GameServers.AsQueryable().Where(g => g.GuildID == guildID).ToListAsync();

        public async Task RemoveServerAsync(IPEndPoint endPoint, ulong guildID)
        {
            GameServer serverToRemove = (await _dbContext.GameServers.AsQueryable().ToListAsync()).FirstOrDefault(x => x.ServerIP.Address.ToString() == endPoint.Address.ToString() && x.ServerIP.Port == endPoint.Port && x.GuildID == guildID);
            if (serverToRemove == null)
            {
                throw new ArgumentException("The specified server does not exist");
            }
            if (serverToRemove.MessageID != null)
            {
                try
                {
                    if (!_discordClient.Guilds.TryGetValue(serverToRemove.GuildID, out DiscordGuild guild))
                    {
                        return;
                    }
                    DiscordChannel channel = guild?.GetChannel(serverToRemove.ChannelID);
                    DiscordMessage msg = await (channel?.GetMessageAsync(serverToRemove.MessageID.Value));
                    if (msg != null)
                    {
                        await msg.DeleteAsync();
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, $"Error trying to remove message for game server {endPoint.Address}");
                }
            }
            _ = _dbContext.GameServers.Remove(serverToRemove);
            _ = await _dbContext.SaveChangesAsync();
        }

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
                string json = await MonkeyHelpers.ReadTextAsync(storedValuesPath);
                List<HistoricData<int>> loadedData = JsonSerializer.Deserialize<List<HistoricData<int>>>(json);
                historicData = loadedData
                    .Where(x => x.Time > minTime)
                    .ToList();
            }

            historicData.Add(new HistoricData<int>(now, Math.Min(currentPlayers, maxPlayers)));

            await MonkeyHelpers.WriteTextAsync(storedValuesPath, JsonSerializer.Serialize(historicData, new JsonSerializerOptions() { WriteIndented = true }))
                ;

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
            var lines = new List<string>
            {
                "      minutes ago",
                "   0┊0┊0┊0┊0┊0┊0┊0┊0┊0",
                "   9┊8┊7┊6┊5┊4┊3┊2┊1┊0"
            };

            int maxI = maxPlayers / interval;

            for (int i = 0; i <= maxI; i++)
            {
                string line = $"{i * interval,2}";
                line += i == 0 ? "┴" : i == maxI ? "┐" : "┤";
                string joinChar = i == 0 ? "┼" : i == maxI ? "┬" : "┊";
                line += string.Join(joinChar,
                    Enumerable
                    .Range(0, 10)
                    .Select(n => roundedPlayerCounts[n])
                    .Select(cnt => (i, cnt) switch
                    {
                        (0, 0) => "─",
                        (_, 0) => " ",
                        (0, _) => "╨",
                        var (ii, c) when ii < c => "║",
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
    }
}