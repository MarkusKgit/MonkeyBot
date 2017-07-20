using System;
using System.Collections.Generic;
using System.Text;

namespace MonkeyBot.Announcements
{
    public class RecurringAnnouncement : Announcement
    {        
        public string CronExpression { get; private set; }

        public RecurringAnnouncement(string id, string cronExpression, string message)
        {
            ID = id;
            CronExpression = cronExpression;
            Message = message;
        }
    }
}
