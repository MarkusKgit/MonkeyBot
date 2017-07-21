using System;

namespace MonkeyBot.Announcements
{
    [Serializable]
    public class SingleAnnouncement : Announcement
    {
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