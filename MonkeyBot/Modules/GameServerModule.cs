using Discord.Commands;
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
    public class GameServerModule : ModuleBase
    {
        private IGameServerService gameServerService;

        public GameServerModule(IGameServerService gameServerService)
        {
            this.gameServerService = gameServerService;
        }

        [Command("Add")]
        [Remarks("Adds the specified game server and sets the channel of the current guild where the info will be posted.")]
        public async Task AddGameServerAsync([Summary("The ip adress and query port of the server e.g. 127.0.0.1:1234")] string ip, [Summary("The name of the channel where the server info should be posted")] string channelName)
        {
            var allChannels = await Context.Guild.GetTextChannelsAsync();
            var channel = allChannels.Where(x => x.Name.ToLower() == channelName.ToLower()).FirstOrDefault();
            if (channel == null)
                await ReplyAsync("The specified channel does not exist");
            else
                await AddGameServerAsync(ip, channel.Id);
        }

        private async Task AddGameServerAsync(string ip, ulong channelID)
        {
            //Do parameter checks
            if (string.IsNullOrEmpty(ip))
            {
                await ReplyAsync("You need to specify an IP-Adress + Port for the server! For example 127.0.0.1:1234");
                return;
            }
            var splitIP = ip.Split(':');
            if (splitIP == null || splitIP.Length != 2)
            {
                await ReplyAsync("You need to specify a valid IP-Adress + Port for the server! For example 127.0.0.1:1234");
                return;
            }
            IPAddress parsedIP = null;
            if (!IPAddress.TryParse(splitIP[0], out parsedIP))
            {
                await ReplyAsync("You need to specify a valid IP-Adress + Port for the server! For example 127.0.0.1:1234");
                return;
            }
            int port = 0;
            if (!int.TryParse(splitIP[1], out port))
            {
                await ReplyAsync("You need to specify a valid IP-Adress + Port for the server! For example 127.0.0.1:1234");
                return;
            }
            IPEndPoint endPoint = new IPEndPoint(parsedIP, port);

            try
            {
                // Add the announcement to the Service to activate it
                await gameServerService.AddServerAsync(endPoint, Context.Guild.Id, channelID);
            }
            catch (ArgumentException ex)
            {
                await Console.Out.WriteLineAsync(ex.Message);
            }
        }
    }
}