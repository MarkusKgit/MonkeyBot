using MonkeyBot.Announcements;
using System;
using System.Collections.Generic;
using System.Text;

namespace MonkeyBot.Services
{
    public interface IAnnouncementService
    {
        AnnouncementList Announcements { get;}
        Action<string> AnnouncementMethod { get; set; }

        void AddRecurringAnnouncement(string ID, string cronExpression, string message);
        void AddSingleAnnouncement(string ID, DateTime excecutionTime, string message);

        void Remove(string ID);

        DateTime GetNextOccurence(string ID);

        void LoadAnnouncements();
        void SaveAnnouncements();
    }
}
