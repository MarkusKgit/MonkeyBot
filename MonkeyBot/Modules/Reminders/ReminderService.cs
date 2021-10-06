using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MonkeyBot.Common;
using MonkeyBot.Database;
using MonkeyBot.Services;

namespace MonkeyBot.Modules.Reminders
{
    /// <summary>
    /// A service that handles reminders
    /// </summary>
    public class ReminderService : IReminderService
    {
        private readonly MonkeyDBContext _dbContext;
        private readonly DiscordClient _discordClient;
        private readonly ISchedulingService _schedulingService;
        private readonly ILogger<ReminderService> _logger;

        public ReminderService(MonkeyDBContext dbContext, DiscordClient discordClient, ISchedulingService schedulingService, ILogger<ReminderService> logger)
        {
            _dbContext = dbContext;
            _discordClient = discordClient;
            _schedulingService = schedulingService;
            _logger = logger;
        }

        public async Task InitializeAsync()
        {
            await RemovePastJobsAsync();
            await BuildJobsAsync();
        }

        /// <summary>
        /// Add an reminder that should be broadcasted regularly based on the interval defined by the Cron Expression
        /// </summary>
        /// <param name="id">The unique ID of the reminder</param>
        /// <param name="cronExpression">The cron expression that defines the broadcast intervall. See https://github.com/atifaziz/NCrontab/wiki/Crontab-Expression for details</param>
        /// <param name="message">The message to be broadcasted</param>
        /// <param name="guildId">The ID of the Guild where the message will be broadcasted</param>
        /// <param name="channelId">The ID of the Channel where the message will be broadcasted</param>
        public Task AddRecurringReminderAsync(string id, string cronExpression, string message, ulong guildId, ulong channelId)
        {
            if (id.IsEmpty())
            {
                throw new ArgumentException("Please provide an ID");
            }
            // Create the reminder, add it to the list and persist it
            var reminder = new Reminder { Type = ReminderType.Recurring, GuildId = guildId, ChannelId = channelId, CronExpression = cronExpression, Name = id, Message = message };
            AddRecurringJob(reminder);
            _dbContext.Reminders.Add(reminder);
            return _dbContext.SaveChangesAsync();
        }

        private void AddRecurringJob(Reminder reminder)
        {
            string id = GetUniqueId(reminder);
            _schedulingService.ScheduleJobRecurring(id, reminder.CronExpression, async () => await RemindAsync(reminder.Message, reminder.GuildId, reminder.ChannelId));
        }

        /// <summary>
        /// Add an reminder that should be broadcasted once on the provided Execution Time
        /// </summary>
        /// <param name="id">The unique ID of the reminder</param>
        /// <param name="excecutionTime">The date and time at which the message should be broadcasted. Must be in the future</param>
        /// <param name="message">The message to be broadcasted</param>
        /// <param name="guildId">The ID of the Guild the message will be sent to</param>
        /// <param name="channelId">The ID of the Channel the message will be sent to</param>
        public Task AddSingleReminderAsync(string id, DateTime excecutionTime, string message, ulong guildId, ulong channelId)
        {
            if (id.IsEmpty())
            {
                throw new ArgumentException("Please provide an ID");
            }
            if (excecutionTime < DateTime.Now)
            {
                throw new ArgumentException("The time you provided is in the past!");
            }
            // Create the reminder, add it to the list and persist it
            var reminder = new Reminder { Type = ReminderType.Once, GuildId = guildId, ChannelId = channelId, ExecutionTime = excecutionTime, Name = id, Message = message };
            AddSingleJob(reminder);
            _dbContext.Reminders.Add(reminder);
            return _dbContext.SaveChangesAsync();
        }

        private void AddSingleJob(Reminder reminder)
        {
            // The reminder's name must be unique on a per guild basis
            string uniqueName = GetUniqueId(reminder);
            // Add a new RunOnce job with the provided ID to the Scheduling Service
            _schedulingService.ScheduleJobOnce(uniqueName, reminder.ExecutionTime.Value, async () => await Task.WhenAll(
                    RemindAsync(reminder.Message, reminder.GuildId, reminder.ChannelId),
                    RemovePastJobsAsync()
                ));
        }

        /// <summary>
        /// Removes the reminder with the provided ID from the list of reminders if it exists
        /// </summary>
        /// <param name="reminderId">The unique ID of the reminder to remove</param>
        /// <param name="guildId">The ID of the guild where the reminder should be removed</param>
        public async Task RemoveAsync(string reminderId, ulong guildId)
        {
            // Try to retrieve the reminder with the provided ID
            Reminder reminder = await GetSpecificReminderAsync(guildId, reminderId);
            if (reminder == null)
            {
                throw new ArgumentException("The reminder with the specified ID does not exist");
            }
            _schedulingService.RemoveJob(GetUniqueId(reminder));
            try
            {
                _dbContext.Reminders.Remove(reminder);
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing reminder");
            }

        }

        /// <summary>
        /// Returns the next execution time of the reminder with the provided ID
        /// </summary>
        /// <param name="reminderId">The unique ID of the reminder</param>
        /// <param name="guildId">ID of the guild where the reminder is posted</param>
        /// <returns>Next execution time</returns>
        public async Task<DateTime> GetNextOccurenceAsync(string reminderId, ulong guildId)
        {
            // Try to retrieve the reminder with the provided ID
            Reminder reminder = await GetSpecificReminderAsync(guildId, reminderId);
            if (reminder == null)
            {
                throw new ArgumentException("The reminder with the specified ID does not exist");
            }
            return _schedulingService.GetNextRun(GetUniqueId(reminder));
        }

        /// <summary>Cleanup method to remove single reminders that are in the past</summary>
        private async Task RemovePastJobsAsync()
        {
            List<Reminder> reminders = await _dbContext.Reminders
                .AsQueryable()
                .Where(x => x.Type == ReminderType.Once && x.ExecutionTime < DateTime.Now)
                .ToListAsync()
                ;
            _dbContext.RemoveRange(reminders);
            await _dbContext.SaveChangesAsync();

        }

        /// <summary>Creates actual jobs from the reminders in the Reminders List to activate them</summary>
        private async Task BuildJobsAsync()
        {
            List<Reminder> reminders = await GetAllRemindersAsync();
            foreach (Reminder reminder in reminders)
            {
                if (reminder.Type == ReminderType.Recurring)
                {
                    AddRecurringJob(reminder);
                }
                else if (reminder.Type == ReminderType.Once)
                {
                    AddSingleJob(reminder);
                }
            }
        }

        private Task RemindAsync(string message, ulong guildId, ulong channelId)
            => MonkeyHelpers.SendChannelMessageAsync(_discordClient, guildId, channelId, message);

        private Task<List<Reminder>> GetAllRemindersAsync()
            => _dbContext.Reminders.AsQueryable().ToListAsync();

        public Task<List<Reminder>> GetRemindersForGuildAsync(ulong guildId)
            => _dbContext.Reminders.AsQueryable().Where(x => x.GuildId == guildId).ToListAsync();

        private Task<Reminder> GetSpecificReminderAsync(ulong guildId, string reminderName)
            => _dbContext.Reminders.AsQueryable().SingleOrDefaultAsync(x => x.GuildId == guildId && x.Name == reminderName);

        // The reminder's name must be unique on a per guild basis
        private static string GetUniqueId(Reminder reminder) => $"{reminder.Name}-{reminder.GuildId}";
    }
}