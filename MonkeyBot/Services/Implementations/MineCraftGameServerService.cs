using Discord;
using Discord.WebSocket;
using dokas.FluentStrings;
using Microsoft.Extensions.Logging;
using MonkeyBot.Services.Common;
using MonkeyBot.Services.Common.MineCraftServerQuery;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MonkeyBot.Services
{
    public class MineCraftGameServerService : BaseGameServerService
    {
        public MineCraftGameServerService(DbService dbService, DiscordSocketClient discordClient, ILogger<MineCraftGameServerService> logger)
            : base(GameServerType.Minecraft, dbService, discordClient, logger)
        {
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
                if (discordGameServer.MessageId.HasValue)
                {
                    if (await channel.GetMessageAsync(discordGameServer.MessageId.Value) is IUserMessage existingMessage && existingMessage != null)
                    {
                        await existingMessage.ModifyAsync(x => x.Embed = builder.Build());
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
                    discordGameServer.MessageId = (await channel?.SendMessageAsync("", false, builder.Build())).Id;
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
    }
}