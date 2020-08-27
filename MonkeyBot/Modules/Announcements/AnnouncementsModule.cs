using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;
using MonkeyBot.Common;
using MonkeyBot.Models;
using MonkeyBot.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MonkeyBot.Modules
{
    /// <summary>Module that provides support for announcements</summary>
    [Group("Announcements")]
    [Description("Announcements")]
    [MinPermissions(AccessLevel.ServerAdmin)]
    [RequireGuild]
    public class AnnouncementsModule : BaseCommandModule
    {
        private readonly IAnnouncementService announcementService;
        private readonly ILogger<AnnouncementsModule> logger;

        public AnnouncementsModule(IAnnouncementService announcementService, ILogger<AnnouncementsModule> logger)
        {
            this.announcementService = announcementService;
            this.logger = logger;
        }

        [Command("AddRecurring")]
        [Description("Adds the specified recurring announcement to the specified channel")]
        [Example("!announcements addrecurring \"weeklyMsg1\" \"0 19 * * 5\" \"It is Friday 19:00\" \"general\"")]
        public async Task AddRecurringAsync(CommandContext ctx, [Description("The id of the announcement.")] string announcementId, [Description("The cron expression to use.")] string cronExpression, [Description("The message to announce.")] string announcement, [Description("Optional: The channel where the announcement should be posted")] DiscordChannel channel = null)
        {            
            if (announcementId.IsEmpty())
            {
                _ = await ctx.ErrorAsync("You need to specify an ID for the Announcement!").ConfigureAwait(false);
                return;
            }

            if (cronExpression.IsEmpty())
            {
                _ = await ctx.ErrorAsync("You need to specify a Cron expression that sets the interval for the Announcement!").ConfigureAwait(false);
                return;
            }

            if (announcement.IsEmpty())
            {
                _ = await ctx.ErrorAsync("You need to specify a message to announce!").ConfigureAwait(false);
                return;
            }

            // ID must be unique per guild -> check if it already exists
            List<Announcement> announcements = await announcementService.GetAnnouncementsForGuildAsync(ctx.Guild.Id).ConfigureAwait(false);
            if (announcements != null && announcements.Any(x => x.Name == announcementId))
            {
                _ = await ctx.ErrorAsync("The ID is already in use").ConfigureAwait(false);
                return;
            }

            channel ??= ctx.Channel ?? ctx.Guild.GetDefaultChannel();

            try
            {
                // Add the announcement to the Service to activate it
                await announcementService.AddRecurringAnnouncementAsync(announcementId, cronExpression, announcement, ctx.Guild.Id, channel.Id).ConfigureAwait(false);
                DateTime nextRun = await announcementService.GetNextOccurenceAsync(announcementId, ctx.Guild.Id).ConfigureAwait(false);
                _ = await ctx.OkAsync($"The announcement has been added. The next run is on {nextRun}").ConfigureAwait(false);
            }
            catch (ArgumentException ex)
            {
                _ = await ctx.ErrorAsync(ex.Message).ConfigureAwait(false);
                logger.LogWarning(ex, "Wrong argument while adding a recurring announcement");
            }
        }

        [Command("AddSingle")]
        [Description("Adds the specified single announcement at the given time to the specified channel")]
        [Example("!announcements addsingle \"reminder1\" \"19:00\" \"It is 19:00\" \"general\"")]
        public async Task AddSingleAsync(CommandContext ctx, [Description("The id of the announcement.")] string announcementId, [Description("The time when the message should be announced.")] string time, [Description("The message to announce.")] string announcement, [Description("Optional: The channel where the announcement should be posted")] DiscordChannel channel = null)
        {            
            // Do parameter checks
            if (announcementId.IsEmpty())
            {
                _ = await ctx.ErrorAsync("You need to specify an ID for the Announcement!").ConfigureAwait(false);
                return;
            }
            if (time.IsEmpty() || !DateTime.TryParse(time, out DateTime parsedTime) || parsedTime < DateTime.Now)
            {
                _ = await ctx.ErrorAsync("You need to specify a date and time for the Announcement that lies in the future!").ConfigureAwait(false);
                return;
            }
            if (announcement.IsEmpty())
            {
                _ = await ctx.ErrorAsync("You need to specify a message to announce!").ConfigureAwait(false);
                return;
            }
            // ID must be unique per guild -> check if it already exists
            List<Announcement> announcements = await announcementService.GetAnnouncementsForGuildAsync(ctx.Guild.Id).ConfigureAwait(false);
            if (announcements.Any(x => x.Name == announcementId))
            {
                _ = await ctx.ErrorAsync("The ID is already in use").ConfigureAwait(false);
                return;
            }
            channel ??= ctx.Channel ?? ctx.Guild.GetDefaultChannel();

            try
            {
                // Add the announcement to the Service to activate it
                await announcementService.AddSingleAnnouncementAsync(announcementId, parsedTime, announcement, ctx.Guild.Id, channel.Id).ConfigureAwait(false);
                DateTime nextRun = await announcementService.GetNextOccurenceAsync(announcementId, ctx.Guild.Id).ConfigureAwait(false);
                _ = await ctx.OkAsync($"The announcement has been added. It will be broadcasted on {nextRun}").ConfigureAwait(false);
            }
            catch (ArgumentException ex)
            {
                _ = await ctx.ErrorAsync(ex.Message).ConfigureAwait(false);
                logger.LogWarning(ex, "Wrong argument while adding a single announcement");
            }
        }

        [Command("List")]
        [Description("Lists all upcoming announcements")]
        public async Task ListAsync()
        {
            List<Announcement> announcements = await announcementService.GetAnnouncementsForGuildAsync(Context.Guild.Id).ConfigureAwait(false);
            string message = announcements.Count == 0 ? "No upcoming announcements" : "The following upcoming announcements exist:";
            var builder = new System.Text.StringBuilder();
            _ = builder.Append(message);
            foreach (Announcement announcement in announcements)
            {
                DateTime nextRun = await announcementService.GetNextOccurenceAsync(announcement.Name, Context.Guild.Id).ConfigureAwait(false);
                IGuildChannel channel = await Context.Guild.GetChannelAsync(announcement.ChannelID).ConfigureAwait(false);
                if (announcement.Type == AnnouncementType.Recurring)
                {
                    _ = builder.AppendLine($"Recurring announcement with ID: \"{announcement.Name}\" will run next at {nextRun.ToString()} in channel {channel?.Name} with message: \"{announcement.Message}\"");
                }
                else if (announcement.Type == AnnouncementType.Once)
                {
                    _ = builder.AppendLine($"Single announcement with ID: \"{announcement.Name}\" will run once at {nextRun.ToString()} in channel {channel?.Name} with message: \"{announcement.Message}\"");
                }
            }
            message = builder.ToString();
            _ = await Context.User.SendMessageAsync(message).ConfigureAwait(false);
            await ReplyAndDeleteAsync("I have sent you a private message").ConfigureAwait(false);
        }

        [Command("Remove")]
        [Description("Removes the announcement with the specified ID")]
        [Example("!announcements remove announcement1")]
        public async Task RemoveAsync([Description("The id of the announcement.")] [RemainingText] string id)
        {
            string cleanID = id.Trim('\"'); // Because the id is flagged with remainder we need to strip leading and trailing " if entered by the user
            if (cleanID.IsEmpty())
            {
                _ = await ctx.RespondAsync("You need to specify the ID of the Announcement you wish to remove!").ConfigureAwait(false);
                return;
            }
            try
            {
                await announcementService.RemoveAsync(cleanID, Context.Guild.Id).ConfigureAwait(false);
                await ReplyAndDeleteAsync("The announcement has been removed!").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _ = await ctx.RespondAsync(ex.Message).ConfigureAwait(false);
            }
        }

        [Command("NextRun")]
        [Description("Gets the next execution time of the announcement with the specified ID.")]
        [Example("!announcements nextrun announcement1")]
        public async Task NextRunAsync([Description("The id of the announcement.")] [RemainingText] string id)
        {
            string cleanID = id.Trim('\"'); // Because the id is flagged with remainder we need to strip leading and trailing " if entered by the user
            if (cleanID.IsEmpty())
            {
                _ = await ctx.RespondAsync("You need to specify an ID for the Announcement!").ConfigureAwait(false);
                return;
            }
            try
            {
                DateTime nextRun = await announcementService.GetNextOccurenceAsync(cleanID, Context.Guild.Id).ConfigureAwait(false);
                _ = await ctx.RespondAsync(nextRun.ToString()).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _ = await ctx.RespondAsync(ex.Message).ConfigureAwait(false);
            }
        }
    }
}