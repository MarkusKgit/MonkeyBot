using System;

namespace MonkeyBot.Models
{
    public class Announcement
    {
        public int ID { get; set; }

        public ulong GuildID { get; set; }

        public ulong ChannelID { get; set; }

        public AnnouncementType Type { get; set; }

        public string Name { get; set; }

        public string Message { get; set; }

        public DateTime? ExecutionTime { get; set; }

        public string CronExpression { get; set; }
    }

    public enum AnnouncementType
    {
        Once,
        Recurring
    }
}