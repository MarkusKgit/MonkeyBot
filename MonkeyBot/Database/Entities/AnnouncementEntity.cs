using System;
using System.ComponentModel.DataAnnotations;

namespace MonkeyBot.Database.Entities
{
    public class AnnouncementEntity : BaseEntity
    {
        [Required]
        public ulong GuildId { get; set; }

        [Required]
        public ulong ChannelId { get; set; }

        public AnnouncementType Type { get; set; }

        public string Name { get; set; }

        public DateTime? ExecutionTime { get; set; }

        public string CronExpression { get; set; }

        public string Message { get; set; }
    }

    public enum AnnouncementType
    {
        Single,
        Recurring
    }
}