using System.ComponentModel.DataAnnotations;

namespace MonkeyBot.Database.Entities
{
    public class GameSubscriptionEntity : BaseGuildEntity
    {
        [Required]
        public ulong UserId { get; set; }

        [Required]
        public string GameName { get; set; }
    }
}