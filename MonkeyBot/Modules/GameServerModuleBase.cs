using Discord.Commands;
using dokas.FluentStrings;
using Microsoft.Extensions.Logging;
using MonkeyBot.Services;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace MonkeyBot.Modules
{
    public abstract class GameServerModuleBase : ModuleBase
    {
        private readonly IGameServerService gameServerService;
        private readonly ILogger<ModuleBase> logger;

        protected GameServerModuleBase(IGameServerService gameServerService, ILogger<ModuleBase> logger)
        {
            this.gameServerService = gameServerService;
            this.logger = logger;
        }

        protected async Task AddGameServerInternalAsync(string ip, ulong channelID)
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

        protected async Task RemoveGameServerInternalAsync(string ip)
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