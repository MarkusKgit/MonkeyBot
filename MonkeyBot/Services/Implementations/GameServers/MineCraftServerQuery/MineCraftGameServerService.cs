using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using MonkeyBot.Common;
using MonkeyBot.Database;
using MonkeyBot.Models;
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
        private readonly MonkeyDBContext dbContext;
        private readonly DiscordSocketClient discordClient;
        private readonly ILogger<MineCraftGameServerService> logger;

        public MineCraftGameServerService(
            MonkeyDBContext dbContext,
            IPictureUploadService pictureUploadService,
            DiscordSocketClient discordClient,
            ILogger<MineCraftGameServerService> logger)
            : base(GameServerType.Minecraft, dbContext, discordClient, logger)
        {
            this.pictureUploadService = pictureUploadService;
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
                EmbedBuilder builder = new EmbedBuilder()
                    .WithColor(new Color(21, 26, 35))
                    .WithTitle($"Minecraft Server ({discordGameServer.ServerIP.Address}:{discordGameServer.ServerIP.Port})")
                    .WithDescription($"Motd: {serverInfo.Description.Motd}");

                if (serverInfo.Players.Sample != null && serverInfo.Players.Sample.Count > 0)
                {
                    _ = builder.AddField($"Online Players ({serverInfo.Players.Online}/{serverInfo.Players.Max})", string.Join(", ", serverInfo.Players.Sample.Select(x => x.Name)));
                }
                else
                {
                    _ = builder.AddField("Online Players", $"{serverInfo.Players.Online}/{serverInfo.Players.Max}");
                }

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

                // Generate chart every full 5 minutes (limit picture upload API calls)
                string pictureUrl = "";
                if (DateTime.Now.Minute % 5 == 0)
                {
                    pictureUrl = await GenerateAndUploadChartAsync(
                        discordGameServer.ServerIP.ToString().Replace(".", "_").Replace(":", "_"),
                        serverInfo.Players.Online,
                        serverInfo.Players.Max).ConfigureAwait(false);

                    if (!pictureUrl.IsEmptyOrWhiteSpace())
                    {
                        _ = builder.WithImageUrl(pictureUrl);
                    }
                }

                if (discordGameServer.MessageID.HasValue)
                {
                    if (await channel.GetMessageAsync(discordGameServer.MessageID.Value).ConfigureAwait(false) is IUserMessage existingMessage && existingMessage != null)
                    {
                        await existingMessage.ModifyAsync(x =>
                        {
                            //Reuse old image url if new one is not set
                            if (pictureUrl.IsEmptyOrWhiteSpace() && existingMessage.Embeds.FirstOrDefault() != null && existingMessage.Embeds.First().Image.HasValue)
                            {
                                _ = builder.WithImageUrl(existingMessage.Embeds.First().Image.Value.Url);
                            }
                            x.Embed = builder.Build();
                        }).ConfigureAwait(false);
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

        private async Task<string> GenerateAndUploadChartAsync(string id, int currentPlayers, int maxPlayers)
        {
            var historyPeriod = TimeSpan.FromHours(12);
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
                List<HistoricData<int>> loadedData = JsonConvert.DeserializeObject<List<HistoricData<int>>>(json);
                historicData = loadedData
                    .Where(x => x.Time > minTime)
                    .ToList();
            }
            // Sharper transition by adding the last known player count with current time stamp and then the new value if the value changed
            if (historicData.Any() && historicData.Last().Value != currentPlayers)
            {
                historicData.Add(new HistoricData<int>(DateTime.Now, historicData.Last().Value));
            }
            historicData.Add(new HistoricData<int>(DateTime.Now, currentPlayers));

            await MonkeyHelpers.WriteTextAsync(storedValuesPath, JsonConvert.SerializeObject(historicData, Formatting.Indented)).ConfigureAwait(false);

            var chart = new XYChart
            {
                AxisX = new ChartAxis
                {
                    Min = 0,
                    Max = (int)historyPeriod.TotalHours,
                    NumTicks = (int)historyPeriod.TotalHours + 1,
                    LabelFunc = (x) => x == (int)historyPeriod.TotalHours ? "Now" : $"{x - historyPeriod.TotalHours}h"
                },
                AxisY = new ChartAxis
                {
                    Min = 0,
                    Max = maxPlayers,
                    NumTicks = 11
                }
            };
            double tickSpan = historyPeriod.TotalHours;

            List<PointF> transformedValues = historicData.Select(d => new PointF((float)d.Time.Subtract(minTime).TotalHours, d.Value))
                                                         .ToList();

            string pictureFilePath = $"{baseFilePath}.png";

            chart.ExportChart(pictureFilePath, transformedValues);

            string pictureUrl = await pictureUploadService.UploadPictureAsync(pictureFilePath, id).ConfigureAwait(false);
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