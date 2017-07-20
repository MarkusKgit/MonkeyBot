using System;
using System.Collections.Generic;
using System.Text;

namespace MonkeyBot.Announcements
{
    public abstract class Announcement
    {
        public string ID { get; protected set; }
        public string Message { get; protected set; }
    }
}
