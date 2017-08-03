using Discord.WebSocket;
using FluentScheduler;
using Microsoft.Extensions.DependencyInjection;
using MonkeyBot.Common;
using MonkeyBot.Services.Common.Announcements;
using NCrontab;
using System;
using System.Collections.Generic;
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
        private DbService db;
        private DiscordSocketClient client;

        /// <summary>A List containing all announcements</summary>
        private List<Announcement> announcements;

        public AnnouncementService(IServiceProvider provider)
        {
            client = provider.GetService<DiscordSocketClient>();
            db = provider.GetService<DbService>();
            announcements = new List<Announcement>();
            var registry = new Registry();
            JobManager.Initialize(registry);
            JobManager.JobEnd += JobManager_JobEnd;
        }

        public async Task InitializeAsync()
        {
            announcements = await GetAnnouncementsAsync();
            await RemovePastJobsAsync();
            BuildJobs();
        }

        private async void JobManager_JobEnd(JobEndInfo obj)
        {
            // When a job is done check if old jobs exist and remove them
            await RemovePastJobsAsync();
            await SaveAnnouncements();
        }

        /// <summary>
        /// Add an announcement that should be broadcasted regularly based on the interval defined by the Cron Expression
        /// </summary>
        /// <param name="name">The unique ID of the announcement</param>
        /// <param name="cronExpression">The cron expression that defines the broadcast intervall. See https://github.com/atifaziz/NCrontab/wiki/Crontab-Expression for details</param>
        /// <param name="message">The message to be broadcasted</param>
        /// <param name="guildID">The ID of the Guild where the message will be broadcasted</param>
        /// <param name="channelID">The ID of the Channel where the message will be broadcasted</param>
        public async Task AddRecurringAnnouncementAsync(string name, string cronExpression, string message, ulong guildID, ulong channelID)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Please provide an ID");
            // Try to parse the CronExpression -> if it fails the cron expression was not valid
            var cnSchedule = CrontabSchedule.TryParse(cronExpression, new CrontabSchedule.ParseOptions() { IncludingSeconds = false });
            if (cnSchedule == null)
                throw new ArgumentException("Cron expression is wrong!");
            // Create the announcement, add it to the list and persist it
            var announcement = new RecurringAnnouncement(name, cronExpression, message, guildID, channelID);
            announcements.Add(announcement);
            AddRecurringJob(announcement);
            await SaveAnnouncements();
        }

        private void AddRecurringJob(RecurringAnnouncement announcement)
        {
            // Add a new recurring job with the provided ID to the Jobmanager. The 5 seconds interval is only a stub and will be overridden.
            JobManager.AddJob(async () => await AnnounceAsync(announcement.Message, announcement.GuildId, announcement.ChannelId), (x) => x.WithName(announcement.Name).ToRunEvery(5).Seconds());
            // Retrieve the schedule from the newly created job
            var schedule = JobManager.AllSchedules.Where(x => x.Name == announcement.Name).FirstOrDefault();
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
        /// <param name="name">The unique ID of the announcement</param>
        /// <param name="excecutionTime">The date and time at which the message should be broadcasted. Must be in the future</param>
        /// <param name="message">The message to be broadcasted</param>
        /// <param name="guildID">The ID of the Guild where the message will be broadcasted</param>
        /// <param name="channelID">The ID of the Channel where the message will be broadcasted</param>
        public async Task AddSingleAnnouncementAsync(string name, DateTime excecutionTime, string message, ulong guildID, ulong channelID)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Please provide an ID");
            if (excecutionTime < DateTime.Now)
                throw new ArgumentException("The time you provided is in the past!");
            // Create the announcement, add it to the list and persist it
            var announcement = new SingleAnnouncement(name, excecutionTime, message, guildID, channelID);
            announcements.Add(announcement);
            AddSingleJob(announcement);
            await SaveAnnouncements();
        }

        private void AddSingleJob(SingleAnnouncement announcement)
        {
            // Add a new RunOnce job with the provided ID to the Jobmanager
            JobManager.AddJob(async () => await AnnounceAsync(announcement.Message, announcement.GuildId, announcement.ChannelId), (x) => x.WithName(announcement.Name).ToRunOnceAt(announcement.ExcecutionTime));
        }

        /// <summary>
        /// Removes the announcement with the provided ID from the list of announcements if it exists
        /// </summary>
        /// <param name="announcementID">The unique ID of the announcement to remove</param>
        /// <param name="guildID">The ID of the guild where the announcement should be removed</param>
        public async Task RemoveAsync(string announcementID, ulong guildID)
        {
            // Try to retrieve the announcement with the provided ID
            var announcement = announcements.Where(x => x.Name.ToLower() == announcementID.ToLower() && x.GuildId == guildID).SingleOrDefault();
            if (announcement == null)
                throw new ArgumentException("The announcement with the specified ID does not exist");
            // Remove the announcement and persist the changes
            announcements.Remove(announcement);
            JobManager.RemoveJob(announcementID);
            using (var uow = db.UnitOfWork)
            {
                await uow.Announcements.RemoveAsync(announcement);
                await uow.CompleteAsync();
            }
            await SaveAnnouncements();
        }

        /// <summary>
        /// Returns the next execution time of the announcement with the provided ID
        /// </summary>
        /// <param name="announcementID">The unique ID of the announcement</param>
        /// <returns>Next execution time</returns>
        public DateTime GetNextOccurence(string announcementID, ulong guildID)
        {
            // Try to retrieve the announcement with the provided ID
            var announcement = announcements.Where(x => x.Name == announcementID && x.GuildId == guildID).SingleOrDefault();
            if (announcement == null)
                throw new ArgumentException("The announcement with the specified ID does not exist");
            var jobs = JobManager.AllSchedules.ToList();
            var job = JobManager.GetSchedule(announcementID);
            return job.NextRun;
        }

        /// <summary>Cleanup method to remove single announcements that are in the past</summary>
        private async Task RemovePastJobsAsync()
        {
            using (var uow = db.UnitOfWork)
            {
                for (int i = announcements.Count - 1; i >= 0; i--)
                {
                    var announcement = announcements[i];
                    if (announcement is SingleAnnouncement && (announcement as SingleAnnouncement).ExcecutionTime < DateTime.Now)
                    {
                        announcements.Remove(announcement);
                        if (JobManager.GetSchedule(announcement.Name) != null)
                            JobManager.RemoveJob(announcement.Name);

                        await uow.Announcements.RemoveAsync(announcement);
                    }
                }
                await uow.CompleteAsync();
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

        /// <summary>Persist the announcements to disk</summary>
        private async Task SaveAnnouncements()
        {
            using (var uow = db.UnitOfWork)
            {
                foreach (var announcement in announcements)
                {
                    await uow.Announcements.AddOrUpdateAsync(announcement);
                    await uow.CompleteAsync();
                }
            }
        }

        private async Task AnnounceAsync(string message, ulong guildID, ulong channelID)
        {
            await Helpers.SendChannelMessageAsync(client, guildID, channelID, message);
        }

        private async Task<List<Announcement>> GetAnnouncementsAsync()
        {
            using (var uow = db.UnitOfWork)
            {
                return await uow.Announcements.GetAllAsync();
            }
        }

        public async Task<List<Announcement>> GetAnnouncementsForGuildAsync(ulong guildID)
        {
            return (await GetAnnouncementsAsync())?.Where(x => x.GuildId == guildID).ToList();
        }
    }
}