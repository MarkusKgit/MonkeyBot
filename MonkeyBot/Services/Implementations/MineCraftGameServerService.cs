using Discord;
using Discord.WebSocket;
using dokas.FluentStrings;
using Microsoft.Extensions.Logging;
using MonkeyBot.Common;
using MonkeyBot.Services.Common;
using MonkeyBot.Services.Common.MineCraftServerQuery;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PointF = System.Drawing.PointF;

namespace MonkeyBot.Services
{
    public class MineCraftGameServerService : BaseGameServerService
    {
        private readonly IPictureUploadService pictureUploadService;

        public MineCraftGameServerService(
            DbService dbService,
            IPictureUploadService pictureUploadService,
            DiscordSocketClient discordClient,
            ILogger<MineCraftGameServerService> logger)
            : base(GameServerType.Minecraft, dbService, discordClient, logger)
        {
            this.pictureUploadService = pictureUploadService;
        }

        protected override async Task<bool> PostServerInfoAsync(DiscordGameServerInfo discordGameServer)
        {
            if (discordGameServer == null)
                return false;
            MineQuery query = null;
            try
            {
                query = new MineQuery(discordGameServer.IP.Address, discordGameServer.IP.Port);
                var serverInfo = await query.GetServerInfoAsync();
                if (serverInfo == null)
                    return false;
                var guild = discordClient?.GetGuild(discordGameServer.GuildId);
                var channel = guild?.GetTextChannel(discordGameServer.ChannelId);
                if (guild == null || channel == null)
                    return false;
                var builder = new EmbedBuilder()
                    .WithColor(new Color(21, 26, 35))
                    .WithTitle($"Minecraft Server ({discordGameServer.IP.Address}:{discordGameServer.IP.Port})")
                    .WithDescription($"Motd: {serverInfo.Description.Motd}");

                if (serverInfo.Players.Sample != null && serverInfo.Players.Sample.Count > 0)
                {
                    builder.AddField($"Online Players ({serverInfo.Players.Online}/{serverInfo.Players.Max})", string.Join(", ", serverInfo.Players.Sample.Select(x => x.Name)));
                }
                else
                {
                    builder.AddField("Online Players", $"{serverInfo.Players.Online}/{serverInfo.Players.Max}");
                }

                if (discordGameServer.GameVersion.IsEmpty())
                {
                    discordGameServer.GameVersion = serverInfo.Version.Name;
                    using (var uow = dbService.UnitOfWork)
                    {
                        await uow.GameServers.AddOrUpdateAsync(discordGameServer);
                        await uow.CompleteAsync();
                    }
                }
                else
                {
                    if (serverInfo.Version.Name != discordGameServer.GameVersion)
                    {
                        discordGameServer.GameVersion = serverInfo.Version.Name;
                        discordGameServer.LastVersionUpdate = DateTime.Now;
                        using (var uow = dbService.UnitOfWork)
                        {
                            await uow.GameServers.AddOrUpdateAsync(discordGameServer);
                            await uow.CompleteAsync();
                        }
                    }
                }
                string lastServerUpdate = "";
                if (discordGameServer.LastVersionUpdate.HasValue)
                    lastServerUpdate = $" (Last update: {discordGameServer.LastVersionUpdate.Value})";

                builder.WithFooter($"Server version: {serverInfo.Version.Name}{lastServerUpdate} || Last check: {DateTime.Now}");

                // Generate chart every full 5 minutes (limit picture upload API calls)
                string pictureUrl = "";
                if (DateTime.Now.Minute % 5 == 0)
                {
                    pictureUrl = await GenerateAndUploadChartAsync(
                        discordGameServer.IP.ToString().Replace(".", "_").Replace(":", "_"),
                        serverInfo.Players.Online,
                        serverInfo.Players.Max);

                    if (!pictureUrl.IsEmpty().OrWhiteSpace())
                    {
                        builder.WithImageUrl(pictureUrl);
                    }
                }

                if (discordGameServer.MessageId.HasValue)
                {
                    if (await channel.GetMessageAsync(discordGameServer.MessageId.Value) is IUserMessage existingMessage && existingMessage != null)
                    {                        
                        await existingMessage.ModifyAsync(x =>
                        {
                            //Reuse old image url if new one is not set
                            if (pictureUrl.IsEmpty().OrWhiteSpace() && x.Embed.IsSpecified && x.Embed.Value.Image.HasValue)
                            {
                                builder.WithImageUrl(x.Embed.Value.Image.Value.Url);
                            }
                            x.Embed = builder.Build();
                            });
                    }
                    else
                    {
                        logger.LogWarning($"Error getting updates for server {discordGameServer.IP}. Original message was removed.");
                        await RemoveServerAsync(discordGameServer.IP, discordGameServer.GuildId);
                        await channel.SendMessageAsync($"Error getting updates for server {discordGameServer.IP}. Original message was removed. Please use the proper remove command to remove the gameserver");
                        return false;
                    }
                }
                else
                {
                    var message = await channel?.SendMessageAsync("", false, builder.Build());
                    discordGameServer.MessageId = message.Id;
                    using (var uow = dbService.UnitOfWork)
                    {
                        await uow.GameServers.AddOrUpdateAsync(discordGameServer);
                        await uow.CompleteAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, $"Error getting updates for server {discordGameServer.IP}");
                throw;
            }
            finally
            {
                if (query != null)
                    query.Dispose();
            }
            return true;
        }

        private async Task<string> GenerateAndUploadChartAsync(string id, int currentPlayers, int maxPlayers)
        {
            const int numHours = 10;
            TimeSpan span = TimeSpan.FromHours(numHours);
            const string folder = "Gameservers";

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);
            string baseFilePath = Path.Combine(folder, id);
            string storedValuesPath = $"{baseFilePath}.txt";

            var now = DateTime.Now;
            var minTime = now.Subtract(span);

            var historicData = new List<HistoricData<int>>();
            if (File.Exists(storedValuesPath))
            {
                string json = await MonkeyHelpers.ReadTextAsync(storedValuesPath);
                List<HistoricData<int>> loadedData = JsonConvert.DeserializeObject<List<HistoricData<int>>>(json);
                historicData = loadedData
                    .Where(x => x.Time > minTime)
                    .ToList();
            }

            historicData.Add(new HistoricData<int>(DateTime.Now, currentPlayers));

            await MonkeyHelpers.WriteTextAsync(storedValuesPath, JsonConvert.SerializeObject(historicData, Formatting.Indented));

            var chart = new XYChart
            {
                AxisX = new ChartAxis
                {
                    Min = 0,
                    Max = numHours,
                    NumTicks = numHours + 1,
                    LabelFunc = (x) => $"{x - numHours}h"
                },
                AxisY = new ChartAxis
                {
                    Min = 0,
                    Max = maxPlayers,
                    NumTicks = 11
                }
            };
            double tickSpan = span.TotalHours;

            List<PointF> transformedValues = historicData
                .Select(d => new PointF(
                    (float)d.Time.Subtract(minTime).TotalHours,
                    d.Value))
                .ToList();

            string pictureFilePath = $"{baseFilePath}.png";

            chart.ExportChart(pictureFilePath, transformedValues);

            string pictureUrl = await pictureUploadService.UploadPictureAsync(pictureFilePath, id);
            return pictureUrl;
        }
    }

    public class HistoricData<T>
    {
        public DateTime Time { get; set; }
        public T Value { get; set; }

        public HistoricData()
        {
        }

        public HistoricData(DateTime time, T value)
        {
            Time = time;
            Value = value;
        }
    }
}