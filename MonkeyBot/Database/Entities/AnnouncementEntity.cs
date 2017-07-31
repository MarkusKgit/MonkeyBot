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

        [Required]
        public AnnouncementType Type { get; set; }

        [Required]
        public string Name { get; set; }

        public DateTime? ExecutionTime { get; set; }

        public string CronExpression { get; set; }

        [Required]
        public string Message { get; set; }
    }

    public enum AnnouncementType
    {
        Single = 0,
        Recurring = 1
    }
}