using Discord;
using Discord.Commands;
using dokas.FluentStrings;
using MonkeyBot.Common;
using MonkeyBot.Preconditions;
using MonkeyBot.Services;
using MonkeyBot.Services.Common.Announcements;
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
    public class AnnouncementsModule : ModuleBase
    {
        private readonly IAnnouncementService announcementService; // The Announcementsservice will get injected in CommandHandler

        public AnnouncementsModule(IAnnouncementService announcementService) // Create a constructor for the announcementservice dependency
        {
            this.announcementService = announcementService;
        }

        [Command("AddRecurring")]
        [Remarks("Adds the specified recurring announcement to the current channel of the current guild.")]
        public async Task AddRecurringAsync([Summary("The id of the announcement.")] string announcementId, [Summary("The cron expression to use.")] string cronExpression, [Summary("The message to announce.")] string announcement)
        {
            await AddRecurringAsync(announcementId, cronExpression, Context.Channel.Id, announcement);
        }

        [Command("AddRecurring")]
        [Remarks("Adds the specified recurring announcement to the specified channel of the current guild.")]
        public async Task AddRecurringAsync([Summary("The id of the announcement.")] string announcementId, [Summary("The cron expression to use.")] string cronExpression, [Summary("The name of the channel where the announcement should be posted")] string channelName, [Summary("The message to announce.")] string announcement)
        {
            var allChannels = await Context.Guild.GetTextChannelsAsync();
            var channel = allChannels.FirstOrDefault(x => x.Name.ToLower() == channelName.ToLower());
            if (channel == null)
                await ReplyAsync("The specified channel does not exist");
            else
                await AddRecurringAsync(announcementId, cronExpression, channel.Id, announcement);
        }

        private async Task AddRecurringAsync(string announcementId, string cronExpression, ulong channelID, string announcement)
        {
            //Do parameter checks
            if (announcementId.IsEmpty())
            {
                await ReplyAsync("You need to specify an ID for the Announcement!");
                return;
            }
            if (cronExpression.IsEmpty())
            {
                await ReplyAsync("You need to specify a Cron expression that sets the interval for the Announcement!");
                return;
            }
            if (announcement.IsEmpty())
            {
                await ReplyAsync("You need to specify a message to announce!");
                return;
            }
            // ID must be unique per guild -> check if it already exists
            var announcements = await announcementService.GetAnnouncementsForGuildAsync(Context.Guild.Id);
            if (announcements?.Where(x => x.Name == announcementId).Count() > 0)
            {
                await ReplyAsync("The ID is already in use");
                return;
            }
            try
            {
                // Add the announcement to the Service to activate it
                await announcementService.AddRecurringAnnouncementAsync(announcementId, cronExpression, announcement, Context.Guild.Id, channelID);
                var nextRun = await announcementService.GetNextOccurenceAsync(announcementId, Context.Guild.Id);
                await ReplyAsync("The announcement has been added. The next run is on " + nextRun.ToString());
            }
            catch (ArgumentException ex)
            {
                await Console.Out.WriteLineAsync(ex.Message);
            }
        }

        [Command("AddSingle")]
        [Remarks("Adds the specified single announcement at the given time to the current channel of the current guild.")]
        public async Task AddSingleAsync([Summary("The id of the announcement.")] string announcementId, [Summary("The time when the message should be announced.")] string time, [Summary("The message to announce.")] string announcement)
        {
            await AddSingleAsync(announcementId, time, Context.Channel.Id, announcement);
        }

        [Command("AddSingle")]
        [Remarks("Adds the specified single announcement at the given time to the specified channel of the current guild.")]
        public async Task AddSingleAsync([Summary("The id of the announcement.")] string announcementId, [Summary("The time when the message should be announced.")] string time, [Summary("The name of the channel where the announcement should be posted")] string channelName, [Summary("The message to announce.")] string announcement)
        {
            var allChannels = await Context.Guild.GetTextChannelsAsync();
            var channel = allChannels.FirstOrDefault(x => x.Name.ToLower() == channelName.ToLower());
            if (channel == null)
                await ReplyAsync("The specified channel does not exist");
            else
                await AddSingleAsync(announcementId, time, channel.Id, announcement);
        }

        private async Task AddSingleAsync(string announcementId, string time, ulong channelID, string announcement)
        {
            // Do parameter checks
            if (announcementId.IsEmpty())
            {
                await ReplyAsync("You need to specify an ID for the Announcement!");
                return;
            }
            if (time.IsEmpty() || !DateTime.TryParse(time, out DateTime parsedTime) || parsedTime < DateTime.Now)
            {
                await ReplyAsync("You need to specify a date and time for the Announcement that lies in the future!");
                return;
            }
            if (announcement.IsEmpty())
            {
                await ReplyAsync("You need to specify a message to announce!");
                return;
            }
            // ID must be unique per guild -> check if it already exists
            var announcements = await announcementService.GetAnnouncementsForGuildAsync(Context.Guild.Id);
            if (announcements.Count(x => x.Name == announcementId) > 0)
            {
                await ReplyAsync("The ID is already in use");
                return;
            }
            try
            {
                // Add the announcement to the Service to activate it
                await announcementService.AddSingleAnnouncementAsync(announcementId, parsedTime, announcement, Context.Guild.Id, channelID);
                var nextRun = await announcementService.GetNextOccurenceAsync(announcementId, Context.Guild.Id);
                await ReplyAsync("The announcement has been added. It will be broadcasted on " + nextRun.ToString());
            }
            catch (ArgumentException ex)
            {
                await Console.Out.WriteLineAsync(ex.Message);
            }
        }

        [Command("List")]
        [Remarks("Lists all upcoming announcements for the current guild")]
        public async Task ListAsync()
        {
            string message;
            var announcements = await announcementService.GetAnnouncementsForGuildAsync(Context.Guild.Id);
            if (announcements.Count == 0)
                message = "No upcoming announcements";
            else
                message = "The following upcoming announcements exist:";
            var builder = new System.Text.StringBuilder();
            builder.Append(message);
            foreach (var announcement in announcements)
            {
                var nextRun = await announcementService.GetNextOccurenceAsync(announcement.Name, Context.Guild.Id);
                var channel = await Context.Guild.GetChannelAsync(announcement.ChannelId);
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
            await Context.User.SendMessageAsync(message);
        }

        [Command("Remove")]
        [Remarks("Removes the announcement with the specified ID from the current guild.")]
        public async Task RemoveAsync([Summary("The id of the announcement.")] [Remainder] string id)
        {
            var cleanID = id.Trim('\"'); // Because the id is flagged with remainder we need to strip leading and trailing " if entered by the user
            if (cleanID.IsEmpty())
            {
                await ReplyAsync("You need to specify the ID of the Announcement you wish to remove!");
                return;
            }
            try
            {
                await announcementService.RemoveAsync(cleanID, Context.Guild.Id);
                await ReplyAsync("The announcement has been removed!");
            }
            catch (Exception ex)
            {
                await ReplyAsync(ex.Message);
            }
        }

        [Command("NextRun")]
        [Remarks("Gets the next execution time of the announcement with the specified ID.")]
        public async Task NextRunAsync([Summary("The id of the announcement.")] [Remainder] string id)
        {
            var cleanID = id.Trim('\"'); // Because the id is flagged with remainder we need to strip leading and trailing " if entered by the user
            if (cleanID.IsEmpty())
            {
                await ReplyAsync("You need to specify an ID for the Announcement!");
                return;
            }
            try
            {
                var nextRun = await announcementService.GetNextOccurenceAsync(cleanID, Context.Guild.Id);
                await ReplyAsync(nextRun.ToString());
            }
            catch (Exception ex)
            {
                await ReplyAsync(ex.Message);
            }
        }
    }
}