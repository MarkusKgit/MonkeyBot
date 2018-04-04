using System;
using System.ComponentModel.DataAnnotations;

namespace MonkeyBot.Database.Entities
{
    public class FeedEntity : BaseGuildEntity
    {
        [Required]
        public ulong ChannelId { get; set; }

        [Required]
        public string URL { get; set; }

        public DateTime? LastUpdate { get; set; }
    }
}