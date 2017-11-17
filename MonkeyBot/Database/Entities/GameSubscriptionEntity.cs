using System.ComponentModel.DataAnnotations;

namespace MonkeyBot.Database.Entities
{
    public class GameSubscriptionEntity : BaseEntity
    {
        [Required]
        public long GuildId { get; set; }

        [Required]
        public long UserId { get; set; }

        [Required]
        public string GameName { get; set; }
    }
}