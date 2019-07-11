using Discord;
using Discord.WebSocket;
using dokas.FluentStrings;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MonkeyBot.Services
{
    public class SteamGameServerService : BaseGameServerService
    {
        public SteamGameServerService(DbService dbService, DiscordSocketClient discordClient, ILogger<SteamGameServerService> logger)
            : base(GameServerType.Steam, dbService, discordClient, logger)
        {
        }

        protected override async Task<bool> PostServerInfoAsync(DiscordGameServerInfo discordGameServer)
        {
            if (discordGameServer == null)
                return false;
            try
            {
                var server = new SteamGameServer(discordGameServer.IP);
                var serverInfo = await server?.GetServerInfoAsync();
                var playerInfo = (await server?.GetPlayersAsync()).Where(x => !x.Name.IsEmpty()).ToList();
                if (serverInfo == null || playerInfo == null)
                    return false;
                var guild = discordClient?.GetGuild(discordGameServer.GuildId);
                var channel = guild?.GetTextChannel(discordGameServer.ChannelId);
                if (guild == null || channel == null)
                    return false;
                var builder = new EmbedBuilder();
                builder.WithColor(new Color(21, 26, 35));
                builder.WithTitle($"{serverInfo.Description} Server ({discordGameServer.IP.Address}:{serverInfo.Port})");
                builder.WithDescription(serverInfo.Name);
                builder.AddField("Online Players", $"{playerInfo.Count}/{serverInfo.MaxPlayers}");
                builder.AddField("Current Map", serverInfo.Map);
                if (playerInfo != null && playerInfo.Count > 0)
                    builder.AddField("Currently connected players:", string.Join(", ", playerInfo.Select(x => x.Name).Where(name => !name.IsEmpty()).OrderBy(x => x)));
                string connectLink = $"steam://rungameid/{serverInfo.GameId}//%20+connect%20{discordGameServer.IP.Address}:{serverInfo.Port}";
                builder.AddField("Connect", $"[Click to connect]({connectLink})");

                if (discordGameServer.GameVersion.IsEmpty())
                {
                    discordGameServer.GameVersion = serverInfo.GameVersion;
                    using (var uow = dbService.UnitOfWork)
                    {
                        await uow.GameServers.AddOrUpdateAsync(discordGameServer);
                        await uow.CompleteAsync();
                    }
                }
                else
                {
                    if (serverInfo.GameVersion != discordGameServer.GameVersion)
                    {
                        discordGameServer.GameVersion = serverInfo.GameVersion;
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
                builder.AddField("Server version", $"{serverInfo.GameVersion}{lastServerUpdate}");

                builder.WithFooter($"Last check: {DateTime.Now}");
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
            return true;
        }
    }
}