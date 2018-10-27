using Discord;
using Discord.Commands;
using Microsoft.Extensions.Logging;
using MonkeyBot.Common;
using MonkeyBot.Preconditions;
using MonkeyBot.Services;
using System.Threading.Tasks;

namespace MonkeyBot.Modules
{
    /// <summary>Module that provides support for game servers that implement the steam server query api</summary>
    [Group("SteamGameServer")]
    [Name("SteamGameServer")]
    [MinPermissions(AccessLevel.ServerAdmin)]
    [RequireContext(ContextType.Guild)]
    [RequireBotPermission(GuildPermission.EmbedLinks)]
    public class SteamGameServerModule : GameServerModuleBase
    {
        public SteamGameServerModule(SteamGameServerService steamGameServerService, ILogger<SteamGameServerModule> logger) : base(steamGameServerService, logger)
        {
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
        public async Task AddGameServerAsync([Summary("The ip adress and query port of the server")] string ip, [Summary("The channel where the server info should be posted")] ITextChannel channel)
        {
            if (channel == null)
                await ReplyAsync("The specified channel does not exist");
            else
                await AddGameServerInternalAsync(ip, channel.Id);
        }

        [Command("Remove")]
        [Remarks("Removes the specified game server")]
        [Example("!gameserver remove 127.0.0.1:1234")]
        public async Task RemoveGameServerAsync([Summary("The ip adress and query port of the server")] string ip)
        {
            await RemoveGameServerInternalAsync(ip);
        }
    }
}