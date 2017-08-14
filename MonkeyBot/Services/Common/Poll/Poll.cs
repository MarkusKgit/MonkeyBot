using Discord;
using System.Collections.Generic;

namespace MonkeyBot.Services.Common.Poll
{
    public class Poll
    {
        public ulong GuildId { get; set; }
        public ulong ChannelId { get; set; }
        public ulong MessageId { get; set; }
        public string Question { get; set; }
        public List<Emoji> Answers { get; set; }
        public Dictionary<ulong, int> UserReactionCount { get; set; }

        public Poll()
        {
            Answers = new List<Emoji>();
            UserReactionCount = new Dictionary<ulong, int>();
        }
    }
}