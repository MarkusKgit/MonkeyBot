using Discord;
using Discord.Commands;
using Microsoft.Extensions.Logging;
using MonkeyBot.Common;
using MonkeyBot.Preconditions;
using MonkeyBot.Services;
using System.Threading.Tasks;

namespace MonkeyBot.Modules
{
    /// <summary>Module that provides support for Minecraft game servers</summary>
    [Group("MineCraftGameServer")]
    [Name("MineCraftGameServer")]
    [MinPermissions(AccessLevel.ServerAdmin)]
    [RequireContext(ContextType.Guild)]
    [RequireBotPermission(GuildPermission.EmbedLinks)]
    public class MineCraftGameServerModule : GameServerModuleBase
    {
        public MineCraftGameServerModule(MineCraftGameServerService gameServerService, ILogger<MineCraftGameServerModule> logger) : base(gameServerService, logger)
        {
        }

        [Command("Add")]
        [Remarks("Adds the specified game server and posts it's info info in the current channel")]
        [Example("!gameserver add 127.0.0.1:1234")]
        public Task AddGameServerAsync([Remainder][Summary("The ip adress and query port of the server")] string ip) 
            => AddGameServerInternalAsync(ip, Context.Channel.Id);

        [Command("Add")]
        [Remarks("Adds the specified game server and sets the channel where the info will be posted.")]
        [Example("!gameserver add \"127.0.0.1:1234\" \"general\"")]
        public async Task AddGameServerAsync([Summary("The ip adress and query port of the server")] string ip, [Summary("The channel where the server info should be posted")] ITextChannel channel)
        {
            if (channel == null)
                await ReplyAsync("The specified channel does not exist").ConfigureAwait(false);
            else
                await AddGameServerInternalAsync(ip, channel.Id).ConfigureAwait(false);
        }

        [Command("Remove")]
        [Remarks("Removes the specified game server")]
        [Example("!gameserver remove 127.0.0.1:1234")]
        public Task RemoveGameServerAsync([Summary("The ip adress and query port of the server")] string ip) 
            => RemoveGameServerInternalAsync(ip);
    }
}