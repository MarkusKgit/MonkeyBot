namespace MonkeyBot.Announcements
{
    /// <summary>
    /// An announcement that can be broadcasted regularly based on a cron schedule
    /// </summary>
    public class RecurringAnnouncement : Announcement
    {
        /// <summary>
        /// The cron expression that controls the interval of the announcement.
        /// See https://github.com/atifaziz/NCrontab/wiki/Crontab-Expression for rules and examples
        /// </summary>
        public string CronExpression { get; set; }

        public RecurringAnnouncement()
        {
        }

        public RecurringAnnouncement(string id, string cronExpression, string message, ulong guildID, ulong channelID)
        {
            ID = id;
            CronExpression = cronExpression;
            Message = message;
            GuildID = guildID;
            ChannelID = channelID;
        }
    }
}