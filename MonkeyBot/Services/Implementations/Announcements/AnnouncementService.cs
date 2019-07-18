using Discord.WebSocket;
using dokas.FluentStrings;
using FluentScheduler;
using Microsoft.EntityFrameworkCore;
using MonkeyBot.Common;
using MonkeyBot.Database;
using MonkeyBot.Models;
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
        private readonly MonkeyDBContext dbContext;
        private readonly DiscordSocketClient discordClient;

        /// <summary>A List containing all announcements</summary>
        //private List<Announcement> announcements;

        public AnnouncementService(MonkeyDBContext dbContext, DiscordSocketClient client)
        {
            this.dbContext = dbContext;
            this.discordClient = client;
            JobManager.JobEnd += JobManager_JobEndAsync;
        }

        public async Task InitializeAsync()
        {
            await RemovePastJobsAsync().ConfigureAwait(false);
            await BuildJobsAsync().ConfigureAwait(false);
        }

        private async void JobManager_JobEndAsync(JobEndInfo obj)
        {
            // When a job is done check if old jobs exist and remove them
            await RemovePastJobsAsync().ConfigureAwait(false);
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
            if (name.IsEmpty())
                throw new ArgumentException("Please provide an ID");
            // Try to parse the CronExpression -> if it fails the cron expression was not valid
            var cnSchedule = CrontabSchedule.TryParse(cronExpression, new CrontabSchedule.ParseOptions { IncludingSeconds = false });
            if (cnSchedule == null)
                throw new ArgumentException("Cron expression is wrong!");
            // Create the announcement, add it to the list and persist it
            var announcement = new Announcement { Type = AnnouncementType.Recurring, GuildID = guildID, ChannelID = channelID, CronExpression = cronExpression, Name = name, Message = message };
            AddRecurringJob(announcement);
            dbContext.Announcements.Add(announcement);
            await dbContext.SaveChangesAsync().ConfigureAwait(false);
        }

        private void AddRecurringJob(Announcement announcement)
        {
            var id = GetUniqueId(announcement);
            // Add a new recurring job with the provided ID to the Jobmanager. The 5 seconds interval is only a stub and will be overridden.
            JobManager.AddJob(async () => await AnnounceAsync(announcement.Message, announcement.GuildID, announcement.ChannelID).ConfigureAwait(false), (x) => x.WithName(id).ToRunEvery(5).Seconds());
            // Retrieve the schedule from the newly created job
            var schedule = JobManager.AllSchedules.FirstOrDefault(x => x.Name == id);
            // Create a cronSchedule with the provided cronExpression
            var cnSchedule = CrontabSchedule.Parse(announcement.CronExpression, new CrontabSchedule.ParseOptions { IncludingSeconds = false });
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
            if (name.IsEmpty())
                throw new ArgumentException("Please provide an ID");
            if (excecutionTime < DateTime.Now)
                throw new ArgumentException("The time you provided is in the past!");
            // Create the announcement, add it to the list and persist it
            var announcement = new Announcement { Type = AnnouncementType.Once, GuildID = guildID, ChannelID = channelID, ExecutionTime = excecutionTime, Name = name, Message = message };
            AddSingleJob(announcement);
            dbContext.Announcements.Add(announcement);
            await dbContext.SaveChangesAsync().ConfigureAwait(false);
        }

        private void AddSingleJob(Announcement announcement)
        {
            // The announcment's name must be unique on a per guild basis
            string uniqueName = GetUniqueId(announcement);
            // Add a new RunOnce job with the provided ID to the Jobmanager
            JobManager.AddJob(async () => await AnnounceAsync(announcement.Message, announcement.GuildID, announcement.ChannelID).ConfigureAwait(false), (x) => x.WithName(uniqueName).ToRunOnceAt(announcement.ExecutionTime.Value));
        }

        /// <summary>
        /// Removes the announcement with the provided ID from the list of announcements if it exists
        /// </summary>
        /// <param name="announcementName">The unique ID of the announcement to remove</param>
        /// <param name="guildID">The ID of the guild where the announcement should be removed</param>
        public async Task RemoveAsync(string announcementName, ulong guildID)
        {
            // Try to retrieve the announcement with the provided ID
            var announcement = await GetSpecificAnnouncementAsync(guildID, announcementName).ConfigureAwait(false);
            if (announcement == null)
                throw new ArgumentException("The announcement with the specified ID does not exist");
            JobManager.RemoveJob(GetUniqueId(announcement));
            dbContext.Announcements.Remove(announcement);
        }

        /// <summary>
        /// Returns the next execution time of the announcement with the provided ID
        /// </summary>
        /// <param name="announcementName">The unique ID of the announcement</param>
        /// <param name="guildID">ID of the guild where the announcement is posted</param>
        /// <returns>Next execution time</returns>
        public async Task<DateTime> GetNextOccurenceAsync(string announcementName, ulong guildID)
        {
            // Try to retrieve the announcement with the provided ID
            var announcement = await GetSpecificAnnouncementAsync(guildID, announcementName).ConfigureAwait(false);
            if (announcement == null)
                throw new ArgumentException("The announcement with the specified ID does not exist");
            var job = JobManager.GetSchedule(GetUniqueId(announcement));
            return job.NextRun;
        }

        /// <summary>Cleanup method to remove single announcements that are in the past</summary>
        private async Task RemovePastJobsAsync()
        {
            var announcements = await dbContext.Announcements.Where(x => x.Type == AnnouncementType.Once && x.ExecutionTime < DateTime.Now).ToListAsync().ConfigureAwait(false);
            dbContext.RemoveRange(announcements);
            await dbContext.SaveChangesAsync().ConfigureAwait(false);

        }

        /// <summary>Creates actual jobs from the announcements in the Announcements List to activate them</summary>
        private async Task BuildJobsAsync()
        {
            var announcements = await GetAllAnnouncementsAsync().ConfigureAwait(false);
            foreach (var announcement in announcements)
            {
                if (announcement.Type == AnnouncementType.Recurring)
                    AddRecurringJob(announcement);
                else if (announcement.Type == AnnouncementType.Once)
                    AddSingleJob(announcement);
            }
        }

        private async Task AnnounceAsync(string message, ulong guildID, ulong channelID)
        {
            await MonkeyHelpers.SendChannelMessageAsync(discordClient, guildID, channelID, message).ConfigureAwait(false);
        }

        private async Task<List<Announcement>> GetAllAnnouncementsAsync()
        {
            return await dbContext.Announcements.ToListAsync().ConfigureAwait(false);
        }

        public async Task<List<Announcement>> GetAnnouncementsForGuildAsync(ulong guildID)
        {
            return await dbContext.Announcements.Where(x => x.GuildID == guildID).ToListAsync().ConfigureAwait(false);
        }

        private async Task<Announcement> GetSpecificAnnouncementAsync(ulong guildID, string announcementName)
        {
            return await dbContext.Announcements.SingleOrDefaultAsync(x => x.GuildID == guildID && x.Name == announcementName).ConfigureAwait(false);
        }

        // The announcment's name must be unique on a per guild basis
        private static string GetUniqueId(Announcement announcement) => $"{announcement.Name}-{announcement.GuildID}";
    }
}