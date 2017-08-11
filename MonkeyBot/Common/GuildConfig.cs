using System.Collections.Generic;

namespace MonkeyBot.Common
{
    public class GuildConfig
    {
        public ulong GuildId { get; set; }

        public string CommandPrefix { get; set; } = Configuration.DefaultPrefix;

        public string WelcomeMessageText { get; set; } = "Welcome to the %server% server, %user%!";

        public List<string> Rules { get; set; } = new List<string>();

        public bool ListenToFeeds { get; set; } = false;

        public List<string> FeedUrls { get; set; } = new List<string>();

        public GuildConfig()
        {
            
        }

        public GuildConfig(ulong guildId)
        {
            GuildId = guildId;
        }
    }
}