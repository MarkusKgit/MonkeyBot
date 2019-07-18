using System;
using System.Net;

namespace MonkeyBot.Models
{
    public class GameServer
    {
        public int ID { get; set; }

        public ulong GuildID { get; set; }

        public ulong ChannelID { get; set; }

        public ulong? MessageID { get; set; }

        public GameServerType GameServerType { get; set; }

        public IPEndPoint ServerIP { get; set; }

        public string GameVersion { get; set; }

        public DateTime? LastVersionUpdate { get; set; }
    }

    public enum GameServerType
    {
        Steam,
        Minecraft
    }
}