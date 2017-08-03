using System.Collections.Generic;

namespace MonkeyBot.Common
{
    public class GuildConfig
    {
        public ulong GuildId { get; set; }

        public string CommandPrefix { get; set; } = Configuration.DefaultPrefix;

        public string WelcomeMessageText { get; set; } = "Welcome to the %server% server, %user%!";

        public List<string> Rules { get; set; }

        public GuildConfig()
        {
            
        }
    }
}