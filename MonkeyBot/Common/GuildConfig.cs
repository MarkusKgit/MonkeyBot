using System.Collections.Generic;

namespace MonkeyBot.Common
{
    public class GuildConfig
    {
        public ulong GuildId { get; set; }

        public string CommandPrefix { get; set; } = Configuration.DefaultPrefix;

        public string WelcomeMessageText { get; set; } = "Welcome to the %server% server, %user%!";

        public bool ListenToFeed { get; set; } = false;

        public string Feedurl { get; set; }

        public List<string> Rules { get; set; }

        public GuildConfig()
        {
            
        }

        public GuildConfig(ulong guildId)
        {
            GuildId = guildId;
        }
    }
}