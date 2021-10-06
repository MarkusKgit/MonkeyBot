using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;
using MonkeyBot.Common;

namespace MonkeyBot.Modules.Reminders
{
    /// <summary>Module that provides support for reminders</summary>
    [Group("Reminders")]
    [Description("Reminders")]
    [MinPermissions(AccessLevel.ServerAdmin)]
    [RequireGuild]
    public class ReminderModule : BaseCommandModule
    {
        private readonly IReminderService _reminderService;
        private readonly ILogger<ReminderModule> _logger;

        public ReminderModule(IReminderService reminderService, ILogger<ReminderModule> logger)
        {
            _reminderService = reminderService;
            _logger = logger;
        }

        [Command("AddRecurring")]
        [Description("Adds the specified recurring reminder to the specified channel")]
        [Example("reminders addrecurring \"weeklyMsg1\" \"0 19 * * 5\" \"It is Friday 19:00\" \"general\"")]
        public async Task AddRecurringAsync(CommandContext ctx, [Description("The id of the reminder.")]
            string reminderId, [Description("The cron expression to use.")]
            string cronExpression, [Description("The message to remind of.")]
            string reminder, [Description("Optional: The channel where the reminder should be posted")]
            DiscordChannel channel = null)
        {
            if (reminderId.IsEmpty())
            {
                await ctx.ErrorAsync("You need to specify an ID for the Reminders!");
                return;
            }

            if (cronExpression.IsEmpty())
            {
                await ctx.ErrorAsync("You need to specify a Cron expression that sets the interval for the Reminders!");
                return;
            }

            if (reminder.IsEmpty())
            {
                await ctx.ErrorAsync("You need to specify a message to announce!");
                return;
            }

            // ID must be unique per guild -> check if it already exists
            List<Reminder> reminders = await _reminderService.GetRemindersForGuildAsync(ctx.Guild.Id);
            if (reminders != null && reminders.Any(x => x.Name == reminderId))
            {
                await ctx.ErrorAsync("The ID is already in use");
                return;
            }

            channel ??= ctx.Channel ?? ctx.Guild.GetDefaultChannel();

            try
            {
                // Add the reminder to the Service to activate it
                await _reminderService.AddRecurringReminderAsync(reminderId, cronExpression, reminder, ctx.Guild.Id,
                    channel.Id);
                DateTime nextRun = await _reminderService.GetNextOccurenceAsync(reminderId, ctx.Guild.Id);
                await ctx.OkAsync($"The reminder has been added. The next run is on {nextRun}");
            }
            catch (ArgumentException ex)
            {
                await ctx.ErrorAsync(ex.Message);
                _logger.LogWarning(ex, "Wrong argument while adding a recurring reminder");
            }
        }

        [Command("AddSingle")]
        [Description("Adds the specified single reminder at the given time to the specified channel")]
        [Example("reminders addsingle \"reminder1\" \"19:00\" \"It is 19:00\" \"general\"")]
        public async Task AddSingleAsync(CommandContext ctx, [Description("The id of the reminder.")]
            string reminderId, [Description("The time when the message should be announced.")]
            string time, [Description("The message to announce.")]
            string reminder, [Description("Optional: The channel where the reminder should be posted")]
            DiscordChannel channel = null)
        {
            // Do parameter checks
            if (reminderId.IsEmpty())
            {
                await ctx.ErrorAsync("You need to specify an ID for the reminder!");
                return;
            }

            if (time.IsEmpty() || !DateTime.TryParse(time, out DateTime parsedTime) || parsedTime < DateTime.Now)
            {
                await ctx.ErrorAsync("You need to specify a date and time for the reminder that lies in the future!");
                return;
            }

            if (reminder.IsEmpty())
            {
                await ctx.ErrorAsync("You need to specify a message to announce!");
                return;
            }

            // ID must be unique per guild -> check if it already exists
            List<Reminder> reminders = await _reminderService.GetRemindersForGuildAsync(ctx.Guild.Id);
            if (reminders.Any(x => x.Name == reminderId))
            {
                await ctx.ErrorAsync("The ID is already in use");
                return;
            }

            channel ??= ctx.Channel ?? ctx.Guild.GetDefaultChannel();
            try
            {
                // Add the reminder to the Service to activate it
                await _reminderService.AddSingleReminderAsync(reminderId, parsedTime, reminder, ctx.Guild.Id,
                    channel.Id);
                DateTime nextRun = await _reminderService.GetNextOccurenceAsync(reminderId, ctx.Guild.Id);
                await ctx.OkAsync($"The reminder has been added. It will be broadcasted on {nextRun}");
            }
            catch (ArgumentException ex)
            {
                await ctx.ErrorAsync(ex.Message);
                _logger.LogWarning(ex, "Wrong argument while adding a single reminder");
            }
        }

        [Command("List")]
        [Description("Lists all upcoming reminders")]
        public async Task ListAsync(CommandContext ctx)
        {
            List<Reminder> reminders = await _reminderService.GetRemindersForGuildAsync(ctx.Guild.Id);
            string message = reminders.Count == 0
                ? "No upcoming reminders"
                : "The following upcoming reminders exist:";
            var builder = new System.Text.StringBuilder();
            builder.Append(message);
            foreach (Reminder reminder in reminders)
            {
                DateTime nextRun = await _reminderService.GetNextOccurenceAsync(reminder.Name, ctx.Guild.Id);
                DiscordChannel channel = ctx.Guild.GetChannel(reminder.ChannelId);
                if (reminder.Type == ReminderType.Recurring)
                {
                    builder.AppendLine(
                        $"Recurring reminder with ID: \"{reminder.Name}\" will run next at {nextRun} in channel {channel?.Name} with message: \"{reminder.Message}\"");
                }
                else if (reminder.Type == ReminderType.Once)
                {
                    builder.AppendLine(
                        $"Single reminder with ID: \"{reminder.Name}\" will run once at {nextRun} in channel {channel?.Name} with message: \"{reminder.Message}\"");
                }
            }

            message = builder.ToString();
            await ctx.RespondDeletableAsync(message);
        }

        [Command("Remove")]
        [Description("Removes the reminder with the specified ID")]
        [Example("reminders remove reminder1")]
        public async Task RemoveAsync(CommandContext ctx, [Description("The id of the reminder.")] [RemainingText]
            string id)
        {
            string
                cleanId = id
                    .Trim('\"'); // Because the id is flagged with remainder we need to strip leading and trailing " if entered by the user
            if (cleanId.IsEmpty())
            {
                await ctx.ErrorAsync("You need to specify the ID of the reminder you wish to remove!");
                return;
            }

            try
            {
                await _reminderService.RemoveAsync(cleanId, ctx.Guild.Id);
                await ctx.OkAsync("The reminder has been removed!");
            }
            catch (Exception ex)
            {
                await ctx.ErrorAsync(ex.Message);
            }
        }

        [Command("NextRun")]
        [Description("Gets the next execution time of the reminder with the specified ID.")]
        [Example("reminders nextrun reminder1")]
        public async Task NextRunAsync(CommandContext ctx, [RemainingText, Description("The id of the reminder.")]
            string id)
        {
            string
                cleanId = id
                    .Trim('\"'); // Because the id is flagged with remainder we need to strip leading and trailing " if entered by the user
            if (cleanId.IsEmpty())
            {
                await ctx.ErrorAsync("You need to specify an ID for the reminder!");
                return;
            }

            try
            {
                List<Reminder> reminders = await _reminderService.GetRemindersForGuildAsync(ctx.Guild.Id);
                Reminder reminder = reminders?.SingleOrDefault(reminder => reminder.Name == cleanId);

                if (reminder == null)
                {
                    await ctx.ErrorAsync("The specified reminder does not exist");
                    return;
                }

                DateTime nextRun = await _reminderService.GetNextOccurenceAsync(cleanId, ctx.Guild.Id);
                await ctx.RespondAsync(nextRun.ToString());

                DiscordChannel channel = ctx.Guild.GetChannel(reminder.ChannelId);

                if (reminder.Type == ReminderType.Recurring)
                {
                    await ctx.RespondAsync(
                        $"Recurring reminder with ID: \"{reminder.Name}\" will run next at {nextRun} in channel {channel?.Name} with message: \"{reminder.Message}\"");
                }
                else if (reminder.Type == ReminderType.Once)
                {
                    await ctx.RespondAsync(
                        $"Single reminder with ID: \"{reminder.Name}\" will run once at {nextRun} in channel {channel?.Name} with message: \"{reminder.Message}\"");
                }
            }
            catch (Exception ex)
            {
                await ctx.ErrorAsync(ex.Message);
            }
        }
    }
}