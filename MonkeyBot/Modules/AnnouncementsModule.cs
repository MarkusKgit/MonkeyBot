using Discord;
using Discord.Commands;
using dokas.FluentStrings;
using Microsoft.Extensions.Logging;
using MonkeyBot.Common;
using MonkeyBot.Preconditions;
using MonkeyBot.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MonkeyBot.Modules
{
    /// <summary>Module that provides support for announcements</summary>
    [Group("Announcements")]
    [Name("Announcements")]
    [MinPermissions(AccessLevel.ServerAdmin)]
    [RequireContext(ContextType.Guild)]
    public class AnnouncementsModule : MonkeyModuleBase
    {
        private readonly IAnnouncementService announcementService;
        private readonly ILogger<AnnouncementsModule> logger;

        public AnnouncementsModule(IAnnouncementService announcementService, ILogger<AnnouncementsModule> logger) // Create a constructor for the announcementservice dependency
        {
            this.announcementService = announcementService;
            this.logger = logger;
        }

        [Command("AddRecurring")]
        [Remarks("Adds the specified recurring announcement to the specified channel")]
        [Example("!announcements addrecurring \"weeklyMsg1\" \"0 19 * * 5\" \"It is Friday 19:00\" \"general\"")]
        public async Task AddRecurringAsync([Summary("The id of the announcement.")] string announcementId, [Summary("The cron expression to use.")] string cronExpression, [Summary("The message to announce.")] string announcement, [Summary("Optional: The name of the channel where the announcement should be posted")] string channelName = "")
        {
            ITextChannel channel = await GetTextChannelInGuildAsync(channelName, true).ConfigureAwait(false);
            if (channel != null)
                await AddRecurringAsync(announcementId, cronExpression, channel.Id, announcement).ConfigureAwait(false);
        }

        private async Task AddRecurringAsync(string announcementId, string cronExpression, ulong channelID, string announcement)
        {
            //Do parameter checks
            if (announcementId.IsEmpty())
            {
                await ReplyAsync("You need to specify an ID for the Announcement!").ConfigureAwait(false);
                return;
            }
            if (cronExpression.IsEmpty())
            {
                await ReplyAsync("You need to specify a Cron expression that sets the interval for the Announcement!").ConfigureAwait(false);
                return;
            }
            if (announcement.IsEmpty())
            {
                await ReplyAsync("You need to specify a message to announce!").ConfigureAwait(false);
                return;
            }
            // ID must be unique per guild -> check if it already exists
            var announcements = await announcementService.GetAnnouncementsForGuildAsync(Context.Guild.Id).ConfigureAwait(false);
            if (announcements?.Where(x => x.Name == announcementId).Count() > 0)
            {
                await ReplyAsync("The ID is already in use").ConfigureAwait(false);
                return;
            }
            try
            {
                // Add the announcement to the Service to activate it
                await announcementService.AddRecurringAnnouncementAsync(announcementId, cronExpression, announcement, Context.Guild.Id, channelID).ConfigureAwait(false);
                var nextRun = await announcementService.GetNextOccurenceAsync(announcementId, Context.Guild.Id).ConfigureAwait(false);
                await ReplyAsync($"The announcement has been added. The next run is on {nextRun}").ConfigureAwait(false);
            }
            catch (ArgumentException ex)
            {
                await ReplyAsync(ex.Message).ConfigureAwait(false);
                logger.LogWarning(ex, "Wrong argument while adding a recurring announcement");
            }
        }

        [Command("AddSingle")]
        [Remarks("Adds the specified single announcement at the given time to the specified channel")]
        [Example("!announcements addsingle \"reminder1\" \"19:00\" \"general\" \"It is 19:00\"")]
        public async Task AddSingleAsync([Summary("The id of the announcement.")] string announcementId, [Summary("The time when the message should be announced.")] string time, [Summary("The message to announce.")] string announcement, [Summary("Optional: The name of the channel where the announcement should be posted")] string channelName = "")
        {
            ITextChannel channel = await GetTextChannelInGuildAsync(channelName, true).ConfigureAwait(false);
            if (channel != null)
                await AddSingleAsync(announcementId, time, channel.Id, announcement).ConfigureAwait(false);
        }

        private async Task AddSingleAsync(string announcementId, string time, ulong channelID, string announcement)
        {
            // Do parameter checks
            if (announcementId.IsEmpty())
            {
                await ReplyAsync("You need to specify an ID for the Announcement!").ConfigureAwait(false);
                return;
            }
            if (time.IsEmpty() || !DateTime.TryParse(time, out DateTime parsedTime) || parsedTime < DateTime.Now)
            {
                await ReplyAsync("You need to specify a date and time for the Announcement that lies in the future!").ConfigureAwait(false);
                return;
            }
            if (announcement.IsEmpty())
            {
                await ReplyAsync("You need to specify a message to announce!").ConfigureAwait(false);
                return;
            }
            // ID must be unique per guild -> check if it already exists
            var announcements = await announcementService.GetAnnouncementsForGuildAsync(Context.Guild.Id).ConfigureAwait(false);
            if (announcements.Count(x => x.Name == announcementId) > 0)
            {
                await ReplyAsync("The ID is already in use").ConfigureAwait(false);
                return;
            }
            try
            {
                // Add the announcement to the Service to activate it
                await announcementService.AddSingleAnnouncementAsync(announcementId, parsedTime, announcement, Context.Guild.Id, channelID).ConfigureAwait(false);
                var nextRun = await announcementService.GetNextOccurenceAsync(announcementId, Context.Guild.Id).ConfigureAwait(false);
                await ReplyAsync($"The announcement has been added. It will be broadcasted on {nextRun}").ConfigureAwait(false);
            }
            catch (ArgumentException ex)
            {
                await ReplyAsync(ex.Message).ConfigureAwait(false);
                logger.LogWarning(ex, "Wrong argument while adding a single announcement");
            }
        }

        [Command("List")]
        [Remarks("Lists all upcoming announcements")]
        public async Task ListAsync()
        {
            string message;
            var announcements = await announcementService.GetAnnouncementsForGuildAsync(Context.Guild.Id).ConfigureAwait(false);
            if (announcements.Count == 0)
                message = "No upcoming announcements";
            else
                message = "The following upcoming announcements exist:";
            var builder = new System.Text.StringBuilder();
            builder.Append(message);
            foreach (var announcement in announcements)
            {
                var nextRun = await announcementService.GetNextOccurenceAsync(announcement.Name, Context.Guild.Id).ConfigureAwait(false);
                var channel = await Context.Guild.GetChannelAsync(announcement.ChannelId).ConfigureAwait(false);
                if (announcement is RecurringAnnouncement)
                {
                    builder.AppendLine($"Recurring announcement with ID: \"{announcement.Name}\" will run next on {nextRun.ToString()} in channel {channel?.Name} with message: \"{announcement.Message}\"");
                }
                else if (announcement is SingleAnnouncement)
                {
                    builder.AppendLine($"Single announcement with ID: \"{announcement.Name}\" will run on {nextRun.ToString()} in channel {channel?.Name} with message: \"{announcement.Message}\"");
                }
            }
            message = builder.ToString();
            await Context.User.SendMessageAsync(message).ConfigureAwait(false);
            await ReplyAndDeleteAsync("I have sent you a private message").ConfigureAwait(false);
        }

        [Command("Remove")]
        [Remarks("Removes the announcement with the specified ID")]
        [Example("!announcements remove announcement1")]
        public async Task RemoveAsync([Summary("The id of the announcement.")] [Remainder] string id)
        {
            var cleanID = id.Trim('\"'); // Because the id is flagged with remainder we need to strip leading and trailing " if entered by the user
            if (cleanID.IsEmpty())
            {
                await ReplyAsync("You need to specify the ID of the Announcement you wish to remove!").ConfigureAwait(false);
                return;
            }
            try
            {
                await announcementService.RemoveAsync(cleanID, Context.Guild.Id).ConfigureAwait(false);
                await ReplyAndDeleteAsync("The announcement has been removed!").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await ReplyAsync(ex.Message).ConfigureAwait(false);
            }
        }

        [Command("NextRun")]
        [Remarks("Gets the next execution time of the announcement with the specified ID.")]
        [Example("!announcements nextrun announcement1")]
        public async Task NextRunAsync([Summary("The id of the announcement.")] [Remainder] string id)
        {
            var cleanID = id.Trim('\"'); // Because the id is flagged with remainder we need to strip leading and trailing " if entered by the user
            if (cleanID.IsEmpty())
            {
                await ReplyAsync("You need to specify an ID for the Announcement!").ConfigureAwait(false);
                return;
            }
            try
            {
                var nextRun = await announcementService.GetNextOccurenceAsync(cleanID, Context.Guild.Id).ConfigureAwait(false);
                await ReplyAsync(nextRun.ToString()).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await ReplyAsync(ex.Message).ConfigureAwait(false);
            }
        }
    }
}