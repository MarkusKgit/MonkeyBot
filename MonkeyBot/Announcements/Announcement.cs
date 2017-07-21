namespace MonkeyBot.Announcements
{
    /// <summary>
    /// Base class for an announcement with common properties
    /// </summary>
    public abstract class Announcement
    {
        /// <summary>The unique ID of the announcement</summary>
        public string ID { get; set; }

        /// <summary>The message that should be broadcasted</summary>
        public string Message { get; set; }
    }
}