using FluentScheduler;
using MonkeyBot.Announcements;
using NCrontab;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace MonkeyBot.Services
{
    /// <summary>
    /// A service that handles announcements
    /// </summary>
    public class AnnouncementService : IAnnouncementService
    {
        private const string persistanceFilename = "Announcements.xml";

        /// <summary>The method that is used to broadcast the announcement. Needs to be set prior usage!</summary>
        public Action<string> AnnouncementMethod { get; set; }

        /// <summary>A List containing all announcements</summary>
        public AnnouncementList Announcements { get; internal set; }

        public AnnouncementService()
        {
            Announcements = new AnnouncementList();
            var registry = new Registry();
            JobManager.Initialize(registry);
            JobManager.JobEnd += JobManager_JobEnd;
        }

        private void JobManager_JobEnd(JobEndInfo obj)
        {
            // When a job is done check if old jobs exist and remove them
            RemovePastJobs();
        }

        /// <summary>
        /// Add an announcement that should be broadcasted regularly based on the interval defined by the Cron Expression
        /// </summary>
        /// <param name="ID">The unique ID of the announcement</param>
        /// <param name="cronExpression">The cron expression that defines the broadcast intervall. See https://github.com/atifaziz/NCrontab/wiki/Crontab-Expression for details</param>
        /// <param name="message">The message to be broadcasted</param>
        public void AddRecurringAnnouncement(string ID, string cronExpression, string message)
        {
            if (string.IsNullOrEmpty(ID))
                throw new ArgumentException("Please provide an ID");
            // Try to parse the CronExpression -> if it fails the cron expression was not valid
            var cnSchedule = CrontabSchedule.TryParse(cronExpression, new CrontabSchedule.ParseOptions() { IncludingSeconds = false });
            if (cnSchedule == null)
                throw new ArgumentException("Cron expression is wrong!");
            // Create the announcement, add it to the list and persist it
            var announcement = new RecurringAnnouncement(ID, cronExpression, message);
            Announcements.Add(announcement);
            AddRecurringJob(announcement);
            SaveAnnouncements();
        }

        private void AddRecurringJob(RecurringAnnouncement announcement)
        {
            if (AnnouncementMethod == null)
                throw new Exception("Announcement method has to be set first!");
            // Add a new recurring job with the provided ID to the Jobmanager. The 5 seconds interval is only a stub and will be overridden.
            JobManager.AddJob(() => AnnouncementMethod(announcement.Message), (x) => x.WithName(announcement.ID).ToRunEvery(5).Seconds());
            // Retrieve the schedule from the newly created job
            var schedule = JobManager.AllSchedules.Where(x => x.Name == announcement.ID).FirstOrDefault();
            // Create a cronSchedule with the provided cronExpression
            var cnSchedule = CrontabSchedule.Parse(announcement.CronExpression, new CrontabSchedule.ParseOptions() { IncludingSeconds = false });
            // Because FluentScheduler does not support cron expressions we have to override the default method that
            // calculates the next run with the appropriate method from the CrontabSchedule scheduler
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
        }

        /// <summary>
        /// Add an announcement that should be broadcasted once on the provided Execution Time
        /// </summary>
        /// <param name="ID">The unique ID of the announcement</param>
        /// <param name="excecutionTime">The date and time at which the message should be broadcasted. Must be in the future</param>
        /// <param name="message">The message to be broadcasted</param>
        public void AddSingleAnnouncement(string ID, DateTime excecutionTime, string message)
        {
            if (string.IsNullOrEmpty(ID))
                throw new ArgumentException("Please provide an ID");
            if (excecutionTime < DateTime.Now)
                throw new ArgumentException("The time you provided is in the past!");
            // Create the announcement, add it to the list and persist it
            var announcement = new SingleAnnouncement(ID, excecutionTime, message);
            Announcements.Add(announcement);
            AddSingleJob(announcement);
            SaveAnnouncements();
        }

        private void AddSingleJob(SingleAnnouncement announcement)
        {
            if (AnnouncementMethod == null)
                return;
            // Add a new RunOnce job with the provided ID to the Jobmanager
            JobManager.AddJob(() => AnnouncementMethod(announcement.Message), (x) => x.WithName(announcement.ID).ToRunOnceAt(announcement.ExcecutionTime));
        }

        /// <summary>
        /// Removes the announcement with the provided ID from the list of announcements if it exists
        /// </summary>
        /// <param name="ID">The unique ID of the announcement to remove</param>
        public void Remove(string ID)
        {
            // Try to retrieve the announcement with the provided ID
            var announcement = Announcements.Where(x => x.ID.ToLower() == ID.ToLower()).SingleOrDefault();
            if (announcement == null)
                throw new ArgumentException("The announcement with the specified ID does not exist");
            // Remove the announcement and persist the changes
            Announcements.Remove(announcement);
            JobManager.RemoveJob(ID);
            SaveAnnouncements();
        }

        /// <summary>
        /// Returns the next execution time of the announcement with the provided ID
        /// </summary>
        /// <param name="ID">The unique ID of the announcement</param>
        /// <returns>Next execution time</returns>
        public DateTime GetNextOccurence(string ID)
        {
            // Try to retrieve the announcement with the provided ID
            var announcement = Announcements.Where(x => x.ID == ID).SingleOrDefault();
            if (announcement == null)
                throw new ArgumentException("The announcement with the specified ID does not exist");
            var jobs = JobManager.AllSchedules.ToList();
            var job = JobManager.GetSchedule(ID);
            return job.NextRun;
        }

        /// <summary>Cleanup method to remove single announcements that are in the past</summary>
        private void RemovePastJobs()
        {
            for (int i = Announcements.Count - 1; i >= 0; i--)
            {
                var announcement = Announcements[i];
                if (announcement is SingleAnnouncement && (announcement as SingleAnnouncement).ExcecutionTime < DateTime.Now)
                {
                    Announcements.Remove(announcement);
                    if (JobManager.GetSchedule(announcement.ID) != null)
                        JobManager.RemoveJob(announcement.ID);
                }
            }
        }

        /// <summary>Creates actual jobs from the announcements in the Announcements List to activate them</summary>
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

        /// <summary>Load the stored announcements</summary>
        public void LoadAnnouncements()
        {
            if (!File.Exists(persistanceFilename))
                return;
            string file = Path.Combine(AppContext.BaseDirectory, persistanceFilename);
            try
            {
                Announcements = JsonConvert.DeserializeObject<AnnouncementList>(File.ReadAllText(file));
                RemovePastJobs();
                BuildJobs();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw ex;
            }
        }

        /// <summary>Persist the announcements to disk</summary>
        public void SaveAnnouncements()
        {
            string file = Path.Combine(AppContext.BaseDirectory, persistanceFilename);
            try
            {
                var json = JsonConvert.SerializeObject(Announcements, Formatting.Indented);
                File.WriteAllText(file, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw ex;
            }
        }
    }
}