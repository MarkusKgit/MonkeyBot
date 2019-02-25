using System.Collections.Generic;

namespace MonkeyBot.Common
{
    public class GuildConfig
    {
        public ulong GuildId { get; set; }

        public string CommandPrefix { get; set; } = Configuration.DefaultPrefix;

        public string WelcomeMessageText { get; set; } = "Welcome to the %server% server, %user%!";

        public ulong WelcomeMessageChannelId { get; set; }

        public string GoodbyeMessageText { get; set; } = "%user% has left %server%. Goodbye!";

        public ulong GoodbyeMessageChannelId { get; set; }

        public List<string> Rules { get; set; } = new List<string>();

        public GuildConfig(ulong guildId)
        {
            GuildId = guildId;
        }
    }
}