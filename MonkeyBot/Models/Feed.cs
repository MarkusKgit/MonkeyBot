using System;

namespace MonkeyBot.Models
{
    public class Feed
    {
        public int ID { get; set; }

        /// <summary>The ID of the Guild where the feed update should be broadcasted</summary>
        public ulong GuildID { get; set; }

        /// <summary>The ID of the Channel where the feed update should be broadcasted</summary>
        public ulong ChannelID { get; set; }

        /// <summary>The name or title of the feed</summary>
        public string? Name { get; set; }

        /// <summary>The URL of the feed</summary>
        public string? URL { get; set; }

        /// <summary>The Time the feed was last updated (null if new)</summary>
        public DateTime? LastUpdate { get; set; }
    }
}