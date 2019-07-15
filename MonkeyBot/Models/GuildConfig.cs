using System.Collections.Generic;

namespace MonkeyBot.Models
{
    public class GuildConfig
    {
        public static readonly string DefaultPrefix = "!";

        public int ID { get; set; }

        public ulong GuildID { get; set; }

        public string CommandPrefix { get; set; } = DefaultPrefix;

        public string WelcomeMessageText { get; set; }

        public ulong WelcomeMessageChannelId { get; set; }

        public string GoodbyeMessageText { get; set; }

        public ulong GoodbyeMessageChannelId { get; set; }

        public List<string> Rules { get; set; }
    }
}