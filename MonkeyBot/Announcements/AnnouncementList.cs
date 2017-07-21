using System.Collections.Generic;
using System.Xml.Serialization;

namespace MonkeyBot.Announcements
{
    [XmlInclude(typeof(RecurringAnnouncement))]
    [XmlInclude(typeof(SingleAnnouncement))]
    public class AnnouncementList : List<Announcement>
    {
    }
}