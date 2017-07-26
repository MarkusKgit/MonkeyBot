using Discord.WebSocket;
using FluentScheduler;
using MonkeyBot.Announcements;
using MonkeyBot.Common;
using NCrontab;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace MonkeyBot.Services
{
    /// <summary>
    /// A service that handles announcements
    /// </summary>
    public class AnnouncementService : IAnnouncementService
    {
        private DiscordSocketClient client;

        private const string persistanceFilename = "Announcements.xml";

        /// <summary>A List containing all announcements</summary>
        private AnnouncementList announcements;

        public AnnouncementService(DiscordSocketClient client)
        {
            this.client = client;
            announcements = new AnnouncementList();
            var registry = new Registry();
            JobManager.Initialize(registry);
            JobManager.JobEnd += JobManager_JobEnd;
            LoadAnnouncementsAsync().Wait(); // Load stored announcements
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
        /// <param name="guildID">The ID of the Guild where the message will be broadcasted</param>
        /// <param name="channelID">The ID of the Channel where the message will be broadcasted</param>
        public async Task AddRecurringAnnouncementAsync(string ID, string cronExpression, string message, ulong guildID, ulong channelID)
        {
            if (string.IsNullOrEmpty(ID))
                throw new ArgumentException("Please provide an ID");
            // Try to parse the CronExpression -> if it fails the cron expression was not valid
            var cnSchedule = CrontabSchedule.TryParse(cronExpression, new CrontabSchedule.ParseOptions() { IncludingSeconds = false });
            if (cnSchedule == null)
                throw new ArgumentException("Cron expression is wrong!");
            // Create the announcement, add it to the list and persist it
            var announcement = new RecurringAnnouncement(ID, cronExpression, message, guildID, channelID);
            announcements.Add(announcement);
            AddRecurringJob(announcement);
            await SaveAnnouncementsAsync();
        }

        private void AddRecurringJob(RecurringAnnouncement announcement)
        {
            // Add a new recurring job with the provided ID to the Jobmanager. The 5 seconds interval is only a stub and will be overridden.
            JobManager.AddJob(async () => await AnnounceAsync(announcement.Message, announcement.GuildID, announcement.ChannelID), (x) => x.WithName(announcement.ID).ToRunEvery(5).Seconds());
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
        /// <param name="guildID">The ID of the Guild where the message will be broadcasted</param>
        /// <param name="channelID">The ID of the Channel where the message will be broadcasted</param>
        public async Task AddSingleAnnouncementAsync(string ID, DateTime excecutionTime, string message, ulong guildID, ulong channelID)
        {
            if (string.IsNullOrEmpty(ID))
                throw new ArgumentException("Please provide an ID");
            if (excecutionTime < DateTime.Now)
                throw new ArgumentException("The time you provided is in the past!");
            // Create the announcement, add it to the list and persist it
            var announcement = new SingleAnnouncement(ID, excecutionTime, message, guildID, channelID);
            announcements.Add(announcement);
            AddSingleJob(announcement);
            await SaveAnnouncementsAsync();
        }

        private void AddSingleJob(SingleAnnouncement announcement)
        {
            // Add a new RunOnce job with the provided ID to the Jobmanager
            JobManager.AddJob(async () => await AnnounceAsync(announcement.Message, announcement.GuildID, announcement.ChannelID), (x) => x.WithName(announcement.ID).ToRunOnceAt(announcement.ExcecutionTime));
        }

        /// <summary>
        /// Removes the announcement with the provided ID from the list of announcements if it exists
        /// </summary>
        /// <param name="announcementID">The unique ID of the announcement to remove</param>
        /// <param name="guildID">The ID of the guild where the announcement should be removed</param>
        public async Task RemoveAsync(string announcementID, ulong guildID)
        {
            // Try to retrieve the announcement with the provided ID
            var announcement = announcements.Where(x => x.ID.ToLower() == announcementID.ToLower() && x.GuildID == guildID).SingleOrDefault();
            if (announcement == null)
                throw new ArgumentException("The announcement with the specified ID does not exist");
            // Remove the announcement and persist the changes
            announcements.Remove(announcement);
            JobManager.RemoveJob(announcementID);
            await SaveAnnouncementsAsync();
        }

        /// <summary>
        /// Returns the next execution time of the announcement with the provided ID
        /// </summary>
        /// <param name="announcementID">The unique ID of the announcement</param>
        /// <returns>Next execution time</returns>
        public DateTime GetNextOccurence(string announcementID, ulong guildID)
        {
            // Try to retrieve the announcement with the provided ID
            var announcement = announcements.Where(x => x.ID == announcementID && x.GuildID == guildID).SingleOrDefault();
            if (announcement == null)
                throw new ArgumentException("The announcement with the specified ID does not exist");
            var jobs = JobManager.AllSchedules.ToList();
            var job = JobManager.GetSchedule(announcementID);
            return job.NextRun;
        }

        /// <summary>Cleanup method to remove single announcements that are in the past</summary>
        private void RemovePastJobs()
        {
            for (int i = announcements.Count - 1; i >= 0; i--)
            {
                var announcement = announcements[i];
                if (announcement is SingleAnnouncement && (announcement as SingleAnnouncement).ExcecutionTime < DateTime.Now)
                {
                    announcements.Remove(announcement);
                    if (JobManager.GetSchedule(announcement.ID) != null)
                        JobManager.RemoveJob(announcement.ID);
                }
            }
        }

        /// <summary>Creates actual jobs from the announcements in the Announcements List to activate them</summary>
        private void BuildJobs()
        {
            JobManager.RemoveAllJobs();
            foreach (var announcement in announcements)
            {
                if (announcement is RecurringAnnouncement)
                    AddRecurringJob(announcement as RecurringAnnouncement);
                else if (announcement is SingleAnnouncement)
                    AddSingleJob(announcement as SingleAnnouncement);
            }
        }

        /// <summary>Load the stored announcements</summary>
        public async Task LoadAnnouncementsAsync()
        {
            string filePath = Path.Combine(AppContext.BaseDirectory, persistanceFilename);
            if (!File.Exists(filePath))
                return;
            try
            {
                var jsonSettings = new JsonSerializerSettings();
                jsonSettings.TypeNameHandling = TypeNameHandling.All;
                string json = await Helpers.ReadTextAsync(filePath);
                announcements = JsonConvert.DeserializeObject<AnnouncementList>(json, jsonSettings);
                RemovePastJobs();
                BuildJobs();
            }
            catch (Exception ex)
            {
                await Console.Out.WriteLineAsync(ex.Message);
                throw ex;
            }
        }

        /// <summary>Persist the announcements to disk</summary>
        public async Task SaveAnnouncementsAsync()
        {
            string filePath = Path.Combine(AppContext.BaseDirectory, persistanceFilename);
            try
            {
                var jsonSettings = new JsonSerializerSettings();
                jsonSettings.TypeNameHandling = TypeNameHandling.All;
                var json = JsonConvert.SerializeObject(announcements, Formatting.Indented, jsonSettings);
                await Helpers.WriteTextAsync(filePath, json);
            }
            catch (Exception ex)
            {
                await Console.Out.WriteLineAsync(ex.Message);
                throw ex;
            }
        }

        private async Task AnnounceAsync(string message, ulong guildID, ulong channelID)
        {
            await Helpers.SendChannelMessage(client, guildID, channelID, message);
        }

        public AnnouncementList GetAnnouncements(ulong guildID)
        {
            return announcements.Where(x => x.GuildID == guildID).ToList() as AnnouncementList;
        }
    }
}