using System;
using System.Net;

namespace MonkeyBot.Services.Common.SteamServerQuery
{
    public class DiscordGameServerInfo
    {
        public IPEndPoint IP { get; private set; }

        public ulong GuildId { get; private set; }

        public ulong ChannelId { get; private set; }

        public ulong? MessageId { get; internal set; }

        public string GameVersion { get; internal set; }

        public DateTime? LastVersionUpdate { get; internal set; }

        public DiscordGameServerInfo()
        {
        }

        public DiscordGameServerInfo(IPEndPoint endpoint, ulong guildID, ulong channelID)
        {
            IP = endpoint;
            GuildId = guildID;
            ChannelId = channelID;
        }
    }
}