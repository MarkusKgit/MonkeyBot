using System;
using Discord.Commands;
using System.Linq;
using System.Threading.Tasks;
using MonkeyBot.Services;

namespace MonkeyBot.Modules
{
    [Group("Announcements")]
    public class AnnouncementsModule : ModuleBase
    {
        private IAnnouncementService announcementService;

        public AnnouncementsModule(IAnnouncementService announcementService)
        {
            this.announcementService = announcementService;
            announcementService.AnnouncementMethod = PostMessageInInfoChannel;
        }
                
        [Command("AddRecurring"), Summary("Adds the specified recurring announcement.")]
        public async Task AddRecurring([Summary("The id of the announcement.")] string id, [Summary("The cron expression to use.")] string cronExpression, [Summary("The message to announce.")] string announcement)
        {
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
            if (announcementService.Announcements.Where(x => x.ID == id).Count() > 0)
            {
                await ReplyAsync("The ID is already in use");
                return;
            }
            try
            {
                announcementService.AddRecurringAnnouncement(id, cronExpression, announcement);                
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        [Command("AddSingle"), Summary("Adds the specified single announcement at the given time.")]
        public async Task AddSingle([Summary("The id of the announcement.")] string id, [Summary("The time when the message should be announced.")] string time, [Summary("The message to announce.")] string announcement)
        {
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
            if (announcementService.Announcements.Where(x => x.ID == id).Count() > 0)
            {
                await ReplyAsync("The ID is already in use");
                return;
            }
            try
            {
                announcementService.AddSingleAnnouncement(id, parsedTime, announcement);
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        [Command("Remove"), Summary("Removes the job with the specified ID.")]
        public async Task Remove([Summary("The id of the announcement.")] [Remainder] string id)
        {
            var cleanID = id.Trim('\"');
            if (string.IsNullOrEmpty(cleanID))
            {
                await ReplyAsync("You need to specify the ID of the Announcement you wish to remove!");
                return;
            }
            try
            {
                announcementService.Remove(cleanID);
            }
            catch (Exception ex)
            {
                await ReplyAsync(ex.Message);
            }
        }

        [Command("NextRun"), Summary("Gets the next execution time of the announcement with the specified ID.")]
        public async Task NextRun([Summary("The id of the announcement.")] [Remainder] string id)
        {
            var cleanID = id.Trim('\"');
            if (string.IsNullOrEmpty(cleanID))
            {
                await ReplyAsync("You need to specify an ID for the Announcement!");
                return;
            }            
            try
            {
                var nextRun = announcementService.GetNextOccurence(cleanID);
                await ReplyAsync(nextRun.ToString());
            }
            catch (Exception ex)
            {
                await ReplyAsync(ex.Message);
            }
        }

        public async void PostMessageInInfoChannel(string message)
        {
            var allTextChannels = await Context.Guild.GetTextChannelsAsync();
            var announcementChannel = allTextChannels.Where(x => x.Name == "rules_and_info").FirstOrDefault();
            if (announcementChannel != null)
                await announcementChannel.SendMessageAsync(message);
        }

    }

    
}
