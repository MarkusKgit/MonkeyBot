using System.Net;

namespace MonkeyBot.Services.Common.SteamServerQuery
{
    public class DiscordGameServerInfo
    {
        public IPEndPoint IP { get; private set; }

        public ulong GuildId { get; private set; }

        public ulong ChannelId { get; private set; }

        public Discord.Rest.RestUserMessage Message { get; internal set; }

        public DiscordGameServerInfo(IPEndPoint endpoint, ulong guildID, ulong channelID)
        {
            IP = endpoint;
            GuildId = guildID;
            ChannelId = channelID;
        }
    }
}