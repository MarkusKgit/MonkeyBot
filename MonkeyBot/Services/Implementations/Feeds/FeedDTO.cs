using System;

namespace MonkeyBot.Services
{
    public class FeedDTO
    {
        /// <summary>The URL of the feed</summary>
        public string URL { get; set; }

        /// <summary>The ID of the Guild where the feed update should be broadcasted</summary>
        public ulong GuildId { get; set; }

        /// <summary>The ID of the Channel where the feed update should be broadcasted</summary>
        public ulong ChannelId { get; set; }

        /// <summary>The Time the feed was last updated</summary>
        public DateTime? LastUpdate { get; set; }
    }
}