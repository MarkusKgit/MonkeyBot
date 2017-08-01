namespace MonkeyBot.Services.Common.Announcements
{
    /// <summary>
    /// Base class for an announcement with common properties
    /// </summary>
    public abstract class Announcement
    {
        /// <summary>The unique ID of the announcement</summary>
        public string Name { get; set; }

        /// <summary>The message that should be broadcasted</summary>
        public string Message { get; set; }

        /// <summary>The ID of the Guild where the message should be broadcasted</summary>
        public ulong GuildId { get; set; }

        /// <summary>The ID of the Channel where the message should be broadcasted</summary>
        public ulong ChannelId { get; set; }
    }
}