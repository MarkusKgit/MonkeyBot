using System.ComponentModel.DataAnnotations;

namespace MonkeyBot.Database.Entities
{
    public class RoleButtonLinkEntity : BaseGuildEntity
    {
        [Required]
        public ulong MessageId { get; set; }

        [Required]
        public ulong RoleId { get; set; }

        [Required]
        public string Emote { get; set; }
    }
}