using System.Collections.Generic;

namespace MonkeyBot.Common
{
    public class GuildConfig
    {
        public ulong GuildId { get; set; }

        public string CommandPrefix { get; set; } = DiscordClientConfiguration.DefaultPrefix;

        public string WelcomeMessageText { get; set; } = "Welcome to the %server% server, %user%!";

        public ulong WelcomeMessageChannelId { get; set; }

        public string GoodbyeMessageText { get; set; } = "%user% has left %server%. Goodbye!";

        public ulong GoodbyeMessageChannelId { get; set; }

        private readonly List<string> rules = new List<string>();

        public IReadOnlyList<string> Rules => rules.AsReadOnly();

        public void AddRule(string rule) => rules.Add(rule);

        public void ClearRules() => rules.Clear();

        public GuildConfig(ulong guildId)
        {
            GuildId = guildId;
        }
    }
}