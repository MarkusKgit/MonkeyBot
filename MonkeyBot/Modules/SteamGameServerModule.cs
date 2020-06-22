using Discord;
using Discord.Commands;
using Microsoft.Extensions.Logging;
using MonkeyBot.Common;
using MonkeyBot.Models;
using MonkeyBot.Preconditions;
using MonkeyBot.Services;
using System.Threading.Tasks;

namespace MonkeyBot.Modules
{
    /// <summary>Module that provides support for game servers that implement the steam server query api</summary>
    [Group("SteamGameServers")]
    [Name("SteamGameServers")]
    [MinPermissions(AccessLevel.ServerAdmin)]
    [RequireContext(ContextType.Guild)]
    [RequireBotPermission(GuildPermission.EmbedLinks)]
    public class SteamGameServerModule : GameServerModuleBase
    {
        public SteamGameServerModule(SteamGameServerService steamGameServerService, ILogger<SteamGameServerModule> logger) : base(steamGameServerService, logger)
        {
        }

        [Command("Add")]
        [Remarks("Adds the specified game server and posts it's info in the current channel")]
        [Example("!steamgameservers add 127.0.0.1:1234")]
        public Task AddGameServerAsync([Remainder][Summary("The ip adress and query port of the server")] string ip)
            => AddGameServerInternalAsync(ip, Context.Channel.Id);

        [Command("Add")]
        [Remarks("Adds the specified game server and sets the channel where the info will be posted.")]
        [Example("!steamgameservers add \"127.0.0.1:1234\" \"general\"")]
        public async Task AddGameServerAsync([Summary("The ip adress and query port of the server")] string ip, [Summary("The channel where the server info should be posted")] ITextChannel channel)
        {
            if (channel == null)
            {
                _ = await ReplyAsync("The specified channel does not exist").ConfigureAwait(false);
            }
            else
            {
                await AddGameServerInternalAsync(ip, channel.Id).ConfigureAwait(false);
            }
        }

        [Command("List")]
        [Remarks("Lists all added steam game servers")]
        [Example("!steamgameservers list")]
        public Task ListSteamGameserversAsync()
            => ListGameServersInternalAsync(GameServerType.Steam);

        [Command("Remove")]
        [Remarks("Removes the specified game server")]
        [Example("!steamgameservers remove 127.0.0.1:1234")]
        public Task RemoveGameServerAsync([Summary("The ip adress and query port of the server")] string ip)
            => RemoveGameServerInternalAsync(ip);
    }
}