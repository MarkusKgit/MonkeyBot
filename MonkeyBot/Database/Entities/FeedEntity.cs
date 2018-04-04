using System;
using System.ComponentModel.DataAnnotations;

namespace MonkeyBot.Database.Entities
{
    public class FeedEntity : BaseEntity
    {
        [Required]
        public ulong GuildId { get; set; }

        [Required]
        public ulong ChannelId { get; set; }

        [Required]
        public string URL { get; set; }

        public DateTime? LastUpdate { get; set; }
    }
}