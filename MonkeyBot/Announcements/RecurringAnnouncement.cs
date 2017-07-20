using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace MonkeyBot.Announcements
{
    [Serializable]
    public class RecurringAnnouncement : Announcement
    {        
        public string CronExpression { get; set; }

        public RecurringAnnouncement()
        {

        }

        public RecurringAnnouncement(string id, string cronExpression, string message)
        {
            ID = id;
            CronExpression = cronExpression;
            Message = message;
        }
    }
}
