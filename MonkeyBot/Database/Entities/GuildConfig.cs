using MonkeyBot.Common;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MonkeyBot.Database.Entities
{
    public class GuildConfig : BaseEntity
    {
        [Required]
        public ulong GuildId { get; set; }

        [Required]
        public string Prefix { get; set; } = Configuration.DefaultPrefix;

        public string WelcomeMessageText { get; set; } = "Welcome to the %server% server, %user%!";

        public List<string> Rules { get; set; }
    }
}