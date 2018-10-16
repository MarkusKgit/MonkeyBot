using Discord;
using System.Collections.Concurrent;
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
        // UserId - count
        public ConcurrentDictionary<int, ulong> ReactionCount { get; set; }

        public Poll()
        {
            Answers = new List<Emoji>();
            ReactionCount = new ConcurrentDictionary<int, ulong>();
        }
    }
}