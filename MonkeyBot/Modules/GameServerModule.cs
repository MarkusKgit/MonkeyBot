using Discord;
using Discord.Commands;
using dokas.FluentStrings;
using Microsoft.Extensions.Logging;
using MonkeyBot.Common;
using MonkeyBot.Preconditions;
using MonkeyBot.Services;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace MonkeyBot.Modules
{
    /// <summary>Module that provides support for game servers that implement the steam server query api</summary>
    [Group("GameServer")]
    [Name("GameServer")]
    [MinPermissions(AccessLevel.ServerAdmin)]
    [RequireContext(ContextType.Guild)]
    [RequireBotPermission(GuildPermission.EmbedLinks)]
    public class GameServerModule : ModuleBase
    {
        private readonly IGameServerService gameServerService;
        private readonly ILogger<GameServerModule> logger;

        public GameServerModule(IGameServerService gameServerService, ILogger<GameServerModule> logger)
        {
            this.gameServerService = gameServerService;
            this.logger = logger;
        }

        [Command("Add")]
        [Remarks("Adds the specified game server and posts it's info info in the current channel")]
        [Example("!gameserver add 127.0.0.1:1234")]
        public async Task AddGameServerAsync([Remainder][Summary("The ip adress and query port of the server")] string ip)
        {
            await AddGameServerInternalAsync(ip, Context.Channel.Id);
        }

        [Command("Add")]
        [Remarks("Adds the specified game server and sets the channel where the info will be posted.")]
        [Example("!gameserver add \"127.0.0.1:1234\" \"general\"")]
        public async Task AddGameServerAsync([Summary("The ip adress and query port of the server")] string ip, [Summary("The name of the channel where the server info should be posted")] string channelName)
        {
            var allChannels = await Context.Guild.GetTextChannelsAsync();
            var channel = allChannels.FirstOrDefault(x => x.Name.ToLower() == channelName.ToLower());
            if (channel == null)
                await ReplyAsync("The specified channel does not exist");
            else
                await AddGameServerInternalAsync(ip, channel.Id);
        }

        private async Task AddGameServerInternalAsync(string ip, ulong channelID)
        {
            //Do parameter checks
            var endPoint = await ParseIPAsync(ip);
            if (endPoint == null)
                return;

            try
            {
                // Add the Server to the Service to activate it
                await gameServerService.AddServerAsync(endPoint, Context.Guild.Id, channelID);
            }
            catch (Exception ex)
            {
                await ReplyAsync($"There was an error while adding the game server:{Environment.NewLine}{ex.Message}");
                logger.LogWarning(ex, "Error adding a gameserver");
            }
            await ReplyAsync("GameServer added");
        }

        [Command("Remove")]
        [Remarks("Removes the specified game server")]
        [Example("!gameserver remove 127.0.0.1:1234")]
        public async Task RemoveGameServerAsync([Summary("The ip adress and query port of the server")] string ip)
        {
            await RemoveGameServerInternalAsync(ip);
        }

        private async Task RemoveGameServerInternalAsync(string ip)
        {
            //Do parameter checks
            var endPoint = await ParseIPAsync(ip);
            if (endPoint == null)
                return;

            try
            {
                // Remove the server from the Service
                await gameServerService.RemoveServerAsync(endPoint, Context.Guild.Id);
            }
            catch (Exception ex)
            {
                await ReplyAsync($"There was an error while trying to remove the game server:{Environment.NewLine}{ex.Message}");
                logger.LogWarning(ex, "Error removing a gameserver");
            }
            await ReplyAsync("GameServer removed");
        }

        private async Task<IPEndPoint> ParseIPAsync(string ip)
        {
            if (ip.IsEmpty())
            {
                await ReplyAsync("You need to specify an IP-Adress + Port for the server! For example 127.0.0.1:1234");
                return null;
            }
            var splitIP = ip.Split(':');
            if (splitIP == null || splitIP.Length != 2)
            {
                await ReplyAsync("You need to specify a valid IP-Adress + Port for the server! For example 127.0.0.1:1234");
                return null;
            }
            if (!IPAddress.TryParse(splitIP[0], out IPAddress parsedIP))
            {
                await ReplyAsync("You need to specify a valid IP-Adress + Port for the server! For example 127.0.0.1:1234");
                return null;
            }
            if (!int.TryParse(splitIP[1], out int port))
            {
                await ReplyAsync("You need to specify a valid IP-Adress + Port for the server! For example 127.0.0.1:1234");
                return null;
            }
            IPEndPoint endPoint = new IPEndPoint(parsedIP, port);
            return endPoint;
        }
    }
}