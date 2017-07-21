using Discord.Commands;
using MonkeyBot.Announcements;
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
    public class AnnouncementsModule : ModuleBase
    {
        private IAnnouncementService announcementService; // The Announcementsservice will get injected in CommandHandler

        public AnnouncementsModule(IAnnouncementService announcementService) // Create a constructor for the announcementservice dependency
        {
            this.announcementService = announcementService;
            announcementService.AnnouncementMethod = PostMessageInAnnouncementChannelAsync; // Set the method used for broadcasting, otherwise it won't work
            try
            {
                announcementService.LoadAnnouncements(); // Load stored announcements
            }
            catch (Exception)
            {
                Console.WriteLine("Announcements could not be loaded");
            }
        }

        [Command("AddRecurring")]
        [Remarks("Adds the specified recurring announcement.")]
        public async Task AddRecurringAsync([Summary("The id of the announcement.")] string id, [Summary("The cron expression to use.")] string cronExpression, [Summary("The message to announce.")] string announcement)
        {
            //Do parameter checks
            if (string.IsNullOrEmpty(id))
            {
                await ReplyAsync("You need to specify an ID for the Announcement!");
                return;
            }
            if (string.IsNullOrEmpty(cronExpression))
            {
                await ReplyAsync("You need to specify a Cron expression that sets the interval for the Announcement!");
                return;
            }
            if (string.IsNullOrEmpty(announcement))
            {
                await ReplyAsync("You need to specify a message to announce!");
                return;
            }
            // ID must be unique -> check if it already exists
            if (announcementService.Announcements.Where(x => x.ID == id).Count() > 0)
            {
                await ReplyAsync("The ID is already in use");
                return;
            }
            try
            {
                // Add the announcement to the Service to activate it
                announcementService.AddRecurringAnnouncement(id, cronExpression, announcement);
                var nextRun = announcementService.GetNextOccurence(id);
                await ReplyAsync("The announcement has been added. The next run is on " + nextRun.ToString());
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        [Command("AddSingle")]
        [Remarks("Adds the specified single announcement at the given time.")]
        public async Task AddSingleAsync([Summary("The id of the announcement.")] string id, [Summary("The time when the message should be announced.")] string time, [Summary("The message to announce.")] string announcement)
        {
            // Do parameter checks
            if (string.IsNullOrEmpty(id))
            {
                await ReplyAsync("You need to specify an ID for the Announcement!");
                return;
            }
            DateTime parsedTime;
            if (string.IsNullOrEmpty(time) || !DateTime.TryParse(time, out parsedTime) || parsedTime < DateTime.Now)
            {
                await ReplyAsync("You need to specify a date and time for the Announcement that lies in the future!");
                return;
            }
            if (string.IsNullOrEmpty(announcement))
            {
                await ReplyAsync("You need to specify a message to announce!");
                return;
            }
            // ID must be unique -> check if it already exists
            if (announcementService.Announcements.Where(x => x.ID == id).Count() > 0)
            {
                await ReplyAsync("The ID is already in use");
                return;
            }
            try
            {
                // Add the announcement to the Service to activate it
                announcementService.AddSingleAnnouncement(id, parsedTime, announcement);
                var nextRun = announcementService.GetNextOccurence(id);
                await ReplyAsync("The announcement has been added. It will be broadcasted on " + nextRun.ToString());
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        [Command("List")]
        [Remarks("Lists all upcoming announcements")]
        public async Task ListAsync()
        {
            string message;
            if (announcementService.Announcements.Count == 0)
                message = "No upcoming announcements";
            else
                message = "The following upcoming announcements exist:";
            foreach (var announcement in announcementService.Announcements)
            {
                var nextRun = announcementService.GetNextOccurence(announcement.ID);
                if (announcement is RecurringAnnouncement)
                {
                    message += Environment.NewLine + string.Format("Recurring announcement with ID: \"{0}\" will run next on {1} with message: \"{2}\"", announcement.ID, nextRun.ToString(), announcement.Message);
                }
                else if (announcement is SingleAnnouncement)
                {
                    message += Environment.NewLine + string.Format("Single announcement with ID: \"{0}\" will run on {1} with message: \"{2}\"", announcement.ID, nextRun.ToString(), announcement.Message);
                }
            }
            await ReplyAsync(message);
        }

        [Command("Remove")]
        [Remarks("Removes the announcement with the specified ID.")]
        public async Task RemoveAsync([Summary("The id of the announcement.")] [Remainder] string id)
        {
            var cleanID = id.Trim('\"'); // Because the id is flagged with remainder we need to strip leading and trailing " if entered by the user
            if (string.IsNullOrEmpty(cleanID))
            {
                await ReplyAsync("You need to specify the ID of the Announcement you wish to remove!");
                return;
            }
            try
            {
                announcementService.Remove(cleanID);
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
            if (string.IsNullOrEmpty(cleanID))
            {
                await ReplyAsync("You need to specify an ID for the Announcement!");
                return;
            }
            try
            {
                var nextRun = announcementService?.GetNextOccurence(cleanID);
                await ReplyAsync(nextRun.ToString());
            }
            catch (Exception ex)
            {
                await ReplyAsync(ex.Message);
            }
        }

        /// <summary>Provides a way to post a message in the current guild's Announcement channel</summary>
        public async void PostMessageInAnnouncementChannelAsync(string message)
        {
            var allTextChannels = await Context.Guild.GetTextChannelsAsync();
            var announcementChannel = allTextChannels.Where(x => x.Name == "rules_and_info").FirstOrDefault();
            if (announcementChannel != null)
                await announcementChannel.SendMessageAsync(message);
        }
    }
}