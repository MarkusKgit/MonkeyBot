using FluentScheduler;
using NCrontab;
using System;
using System.Linq;
using System.Reflection;
using MonkeyBot.Announcements;

using System.Collections.Generic;
using System.Xml.Serialization;
using System.IO;

namespace MonkeyBot.Services
{
    public class AnnouncementService : IAnnouncementService
    {
        const string persistanceFilename = "announcements.xml";

        private Registry registry;

        public Action<string> AnnouncementMethod { get; set; }

        public AnnouncementList Announcements { get; internal set; }

        public AnnouncementService()
        {
            Announcements = new AnnouncementList();
            registry = new Registry();
            JobManager.Initialize(registry);
            LoadAnnouncements();
        }
        
        public void AddRecurringAnnouncement(string jobID, string cronExpression, string message)
        {
            if (string.IsNullOrEmpty(jobID))
                throw new ArgumentException("Please provide an ID");
            var cnSchedule = CrontabSchedule.TryParse(cronExpression, new CrontabSchedule.ParseOptions() { IncludingSeconds = false });
            if (cnSchedule == null)
                throw new ArgumentException("Cron expression is wrong!");
            var announcement = new RecurringAnnouncement(jobID, cronExpression, message);
            Announcements.Add(announcement);
            AddRecurringJob(announcement);
        }
        
        private void AddRecurringJob(RecurringAnnouncement announcement)
        {
            if (AnnouncementMethod == null)
                return;
            JobManager.AddJob(() => AnnouncementMethod(announcement.Message), (x) => x.WithName(announcement.ID).ToRunEvery(5).Seconds());
            var schedule = JobManager.AllSchedules.Where(x => x.Name == announcement.ID).FirstOrDefault();
            var cnSchedule = CrontabSchedule.Parse(announcement.CronExpression, new CrontabSchedule.ParseOptions() { IncludingSeconds = false });
            if (schedule != null)
            {
                var scheduleType = schedule.GetType();
                scheduleType
                  .GetProperty("CalculateNextRun", BindingFlags.NonPublic | BindingFlags.Instance)
                  .SetValue(schedule, (Func<DateTime, DateTime>)cnSchedule.GetNextOccurrence, null);
                scheduleType
                  .GetProperty("NextRun", BindingFlags.Public | BindingFlags.Instance)
                  .SetValue(schedule, cnSchedule.GetNextOccurrence(DateTime.Now));
            }
            SaveAnnouncements();
        }

        public void AddSingleAnnouncement(string jobID, DateTime excecutionTime, string message)
        {
            if (string.IsNullOrEmpty(jobID))
                throw new ArgumentException("Please provide an ID");
            if (excecutionTime < DateTime.Now)
                throw new ArgumentException("The time you provided is in the past!");
            var announcement = new SingleAnnouncement(jobID, excecutionTime, message);
            Announcements.Add(announcement);
            AddSingleJob(announcement);
        }

        private void AddSingleJob(SingleAnnouncement announcement)
        {
            if (AnnouncementMethod == null)
                return;
            JobManager.AddJob(() => AnnouncementMethod(announcement.Message), (x) => x.ToRunOnceAt(announcement.ExcecutionTime));
            SaveAnnouncements();
        }

        public void Remove(string ID)
        {
            var announcement = Announcements.Where(x => x.ID == ID).SingleOrDefault();
            if (announcement == null)
                throw new ArgumentException("The announcement with the specified ID does not exist");            
            Announcements.Remove(announcement);
            JobManager.RemoveJob(ID);
        }

        public DateTime GetNextOccurence(string ID)
        {
            var announcement = Announcements.Where(x => x.ID == ID).SingleOrDefault();
            if (announcement == null)
                throw new ArgumentException("The announcement with the specified ID does not exist");
            var job = JobManager.GetSchedule(ID);
            return job.NextRun;
        }

        private void BuildJobs()
        {
            JobManager.RemoveAllJobs();
            foreach (var announcement in Announcements)
            {
                if (announcement is RecurringAnnouncement)
                    AddRecurringJob(announcement as RecurringAnnouncement);
                else if (announcement is SingleAnnouncement)
                    AddSingleJob(announcement as SingleAnnouncement);
            }
        }

        public void LoadAnnouncements()
        {
            if (!File.Exists(persistanceFilename))
                return;
            XmlSerializer xs = new XmlSerializer(typeof(AnnouncementList));
            var file = File.OpenRead(persistanceFilename);
            Announcements = (AnnouncementList)xs.Deserialize(file);
            BuildJobs();
        }

        public void SaveAnnouncements()
        {
            XmlSerializer xs = new XmlSerializer(typeof(AnnouncementList));
            var file = File.Create(persistanceFilename);
            TextWriter tw = new StreamWriter(file);
            xs.Serialize(tw, Announcements);          
        }        
    }
}
