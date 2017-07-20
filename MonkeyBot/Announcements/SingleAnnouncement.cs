using System;
using System.Collections.Generic;
using System.Text;

namespace MonkeyBot.Announcements
{
    public class SingleAnnouncement : Announcement
    {
        public DateTime ExcecutionTime { get; private set; }

        public SingleAnnouncement(string id, DateTime executionTime, string message)
        {
            ID = id;
            ExcecutionTime = executionTime;
            Message = message;
        }
    }
}
