using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace MonkeyBot.Announcements
{
    [Serializable]
    public abstract class Announcement
    {
        
        public string ID { get; set; }
        
        public string Message { get;  set; }
    }
}
