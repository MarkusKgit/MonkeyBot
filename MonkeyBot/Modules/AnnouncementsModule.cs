using System;
using System.Collections.Generic;
using System.Text;
using Discord;
using Discord.Commands;
using FluentScheduler;
using NCrontab;
using System.Reflection;
using System.Linq;
using System.Threading.Tasks;

namespace MonkeyBot.Modules
{
    //var registry = new Registry();
    //JobManager.Initialize(registry);

    //        string cron = "1 * * * * *";

    //TODO add service to initialize JobManager
    //TODO add serializer for Announcements

    [Group("Announcements")]
    public class AnnouncementsModule : ModuleBase
    {
        // ~Roles Add -role-
        [Command("AddRecurring"), Summary("Adds the specified recurring announcement.")]
        public async Task Add([Summary("The role to add.")] string id, [Summary("The cron expression to use.")] string cronExpression, [Summary("The message to announce.")] [Remainder] string announcement)
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
                await ReplyAsync("You need to specify a message to announce!!");
                return;
            }
            try
            {
                SetCronJob(id, cronExpression, () => PostMessageInInfoChannel(announcement));
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public async void PostMessageInInfoChannel(string message)
        {
            var allTextChannels = await Context.Guild.GetTextChannelsAsync();
            var announcementChannel = allTextChannels.Where(x => x.Name == "rules_and_info").FirstOrDefault();
            if (announcementChannel != null)
                await announcementChannel.SendMessageAsync(message);
        }

        private static void SetCronJob(string id, string cronExpression, Action job)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("Please provide an ID");
            var cnSchedule = CrontabSchedule.TryParse(cronExpression, new CrontabSchedule.ParseOptions() { IncludingSeconds = true });
            if (cnSchedule == null)
                throw new ArgumentException("Cron expression is wrong!");
            JobManager.AddJob(job, (x) => x.WithName(id).ToRunEvery(5).Seconds());
            var schedule = JobManager.AllSchedules.Where(x => x.Name == id).FirstOrDefault();
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

            //var next = cnSchedule.GetNextOccurrences(DateTime.Now, DateTime.Now.AddMinutes(10));
            //string nextOccurences = "Next Occurences:";
            //foreach (var item in next)
            //{
            //    nextOccurences += Environment.NewLine + item;
            //}
            //Console.WriteLine(nextOccurences);
        }
    }

    
}
