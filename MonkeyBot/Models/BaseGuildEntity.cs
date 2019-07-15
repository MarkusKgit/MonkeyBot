using System.ComponentModel.DataAnnotations;

namespace MonkeyBot.Database.Entities
{
    public class BaseGuildEntity : BaseEntity
    {
        [Required]
        public ulong GuildId { get; set; }
    }
}