using MonkeyBot.Announcements;
using System;

namespace MonkeyBot.Services
{
    public interface IAnnouncementService
    {
        AnnouncementList Announcements { get; }
        Action<string> AnnouncementMethod { get; set; }

        void AddRecurringAnnouncement(string ID, string cronExpression, string message);

        void AddSingleAnnouncement(string ID, DateTime excecutionTime, string message);

        void Remove(string ID);

        DateTime GetNextOccurence(string ID);

        void LoadAnnouncements();

        void SaveAnnouncements();
    }
}