using Discord;
using Discord.Rest;
using Discord.WebSocket;
using dokas.FluentStrings;
using Microsoft.Extensions.Logging;
using MonkeyBot.Services.Common;
using MonkeyBot.Services.Common.MineCraftServerQuery;
using System;
using System.Threading.Tasks;

namespace MonkeyBot.Services
{
    public class MineCraftGameServerService : BaseGameServerService
    {
        public MineCraftGameServerService(DbService dbService, DiscordSocketClient discordClient, ILogger<MineCraftGameServerService> logger)
            : base(GameServerType.Minecraft, dbService, discordClient, logger)
        {
        }

        protected override async Task PostServerInfoAsync(DiscordGameServerInfo discordGameServer)
        {
            if (discordGameServer == null)
                return;
            try
            {
                var ms = new MineQuery("37.114.55.252", 25565);
                var stats = await ms.GetStatsAsync();
                if (stats == null)
                    return;
                var guild = discordClient?.GetGuild(discordGameServer.GuildId);
                var channel = guild?.GetTextChannel(discordGameServer.ChannelId);
                if (guild == null || channel == null)
                    return;
                var builder = new EmbedBuilder()
                    .WithColor(new Color(21, 26, 35))
                    .WithTitle($"Minecraft Server ({discordGameServer.IP.Address}:{discordGameServer.IP.Port})")
                    .WithDescription($"Motd: {stats.Motd}")
                    .AddField("Online Players", $"{stats.CurrentPlayers}/{stats.MaximumPlayers}");

                if (discordGameServer.GameVersion.IsEmpty())
                {
                    discordGameServer.GameVersion = stats.Version;
                    using (var uow = dbService.UnitOfWork)
                    {
                        await uow.GameServers.AddOrUpdateAsync(discordGameServer);
                        await uow.CompleteAsync();
                    }
                }
                else
                {
                    if (stats.Version != discordGameServer.GameVersion)
                    {
                        discordGameServer.GameVersion = stats.Version;
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
                builder.AddField("Server version", $"{stats.Version}{lastServerUpdate}");

                builder.WithFooter($"Last check: {DateTime.Now}");                
                if (discordGameServer.MessageId.HasValue)
                {
                    var existingMessage = await channel.GetMessageAsync(discordGameServer.MessageId.Value) as Discord.Rest.RestUserMessage;
                    if (existingMessage != null)
                    {
                        await existingMessage.ModifyAsync(x => x.Embed = builder.Build());
                    }
                    else
                    {
                        logger.LogWarning($"Error getting updates for server {discordGameServer.IP}. Original message was removed.");
                        await RemoveServerAsync(discordGameServer.IP, discordGameServer.GuildId);
                        await channel.SendMessageAsync($"Error getting updates for server {discordGameServer.IP}. Original message was removed. Please use the proper remove command to remove the gameserver");
                        return;
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
        }
    }
}