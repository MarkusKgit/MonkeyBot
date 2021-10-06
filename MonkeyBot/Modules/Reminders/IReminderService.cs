using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MonkeyBot.Modules.Reminders
{
    /// <summary>Interface for a minimum reminder service that handles single and recurring reminders</summary>
    public interface IReminderService
    {
        /// <summary>
        /// A method that will add an reminder that will be sent regularly based on the interval defined as a Cron Expression
        /// It will require a way to schedule to reminder
        /// </summary>
        /// <param name="id">The unique ID of the reminder</param>
        /// <param name="cronExpression">The cron expression that defines the broadcast interval.</param>
        /// <param name="message">The message to be sent</param>
        /// <param name="guildId">The ID of the Guild the message will be sent to</param>
        /// <param name="channelId">The ID of the Channel the message will be sent to</param>
        Task AddRecurringReminderAsync(string id, string cronExpression, string message, ulong guildId, ulong channelId);

        /// <summary>
        /// A method that will add an reminder that will be sent once on the provided Execution Time
        /// It will require a way to schedule to reminder
        /// </summary>
        /// <param name="id">The unique ID of the reminder</param>
        /// <param name="excecutionTime">The date and time at which the message should be sent.</param>
        /// <param name="message">The message to be sent</param>
        /// <param name="guildId">The ID of the Guild the message will be sent to</param>
        /// <param name="channelId">The ID of the Channel the message will be sent to</param>
        Task AddSingleReminderAsync(string id, DateTime excecutionTime, string message, ulong guildId, ulong channelId);

        /// <summary>A method that provides a way to remove an reminder</summary>
        Task RemoveAsync(string reminderId, ulong guildId);

        /// <summary>
        /// A method that returns the next execution time of the reminder with the given ID
        /// </summary>
        /// <returns>Next execution time</returns>
        Task<DateTime> GetNextOccurenceAsync(string reminderId, ulong guildId);

        /// <summary>Returns all reminders of the current guild</summary>
        Task<List<Reminder>> GetRemindersForGuildAsync(ulong guildId);

        Task InitializeAsync();
    }
}