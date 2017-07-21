using System;

namespace MonkeyBot.Announcements
{
    [Serializable]
    public abstract class Announcement
    {
        public string ID { get; set; }
        public string Message { get; set; }
    }
}