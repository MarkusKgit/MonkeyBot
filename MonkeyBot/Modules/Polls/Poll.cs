using System;
using System.Collections.Generic;

namespace MonkeyBot.Models
{
    public class Poll
    {
        public int? Id { get; set; }

        public ulong GuildId { get; set; }

        public ulong ChannelId { get; set; }

        public ulong MessageId { get; set; }

        public ulong CreatorId { get; set; }

        public string Question { get; set; }

        public List<PollAnswer> PossibleAnswers { get; set; }

        public DateTime EndTimeUTC { get; set; }

        public Poll()
        {
        }

        public Poll(ulong guildId, ulong channelId, ulong messageId, ulong pollCreatorId, string question, IEnumerable<PollAnswer> answers, DateTime endTimeUTC)
        {
            GuildId = guildId;
            ChannelId = channelId;
            MessageId = messageId;
            CreatorId = pollCreatorId;
            Question = question;
            PossibleAnswers = new List<PollAnswer>(answers);
            EndTimeUTC = endTimeUTC;
        }
    }
}
