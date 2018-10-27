using System;
using System.Net;

namespace MonkeyBot.Services.Common
{
    public class DiscordGameServerInfo
    {
        public GameServerType GameServerType { get; private set; }

        public IPEndPoint IP { get; private set; }

        public ulong GuildId { get; private set; }

        public ulong ChannelId { get; private set; }

        public ulong? MessageId { get; internal set; }

        public string GameVersion { get; internal set; }

        public DateTime? LastVersionUpdate { get; internal set; }

        public DiscordGameServerInfo()
        {
        }

        public DiscordGameServerInfo(GameServerType gameServerType, IPEndPoint endpoint, ulong guildID, ulong channelID)
        {
            GameServerType = gameServerType;
            IP = endpoint;
            GuildId = guildID;
            ChannelId = channelID;
        }
    }
}