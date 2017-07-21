using System;

namespace MonkeyBot.Announcements
{
    /// <summary>
    /// A single announcement that will only be broadcasted once on the Execution Time
    /// </summary>
    public class SingleAnnouncement : Announcement
    {
        /// <summary>Defines when the message should be broadcasted</summary>
        public DateTime ExcecutionTime { get; set; }

        public SingleAnnouncement()
        {
        }

        public SingleAnnouncement(string id, DateTime executionTime, string message)
        {
            ID = id;
            ExcecutionTime = executionTime;
            Message = message;
        }
    }
}