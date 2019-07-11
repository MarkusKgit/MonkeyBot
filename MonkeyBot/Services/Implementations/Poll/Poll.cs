using Discord;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace MonkeyBot.Services
{
    public class Poll
    {
        public ulong GuildId { get; set; }
        public ulong ChannelId { get; set; }
        public ulong MessageId { get; set; }
        public string Question { get; set; }
        public List<PollAnswer> Answers { get; set; }
        public ConcurrentDictionary<PollAnswer, List<IUser>> ReactionUsers { get; set; }

        public Poll()
        {
            Answers = new List<PollAnswer>();
            ReactionUsers = new ConcurrentDictionary<PollAnswer, List<IUser>>();
        }
    }

    public class PollAnswer : IEquatable<PollAnswer>
    {
        public string Answer { get; set; }
        public Emoji AnswerEmoji { get; set; }

        public PollAnswer(string answer, Emoji answerEmoji)
        {
            Answer = answer;
            AnswerEmoji = answerEmoji;
        }

        public bool Equals(PollAnswer other)
        {
            return
                Answer == other.Answer &&
                AnswerEmoji.Equals(other.AnswerEmoji);
        }

        public override bool Equals(object other)
        {
            return other is PollAnswer && Equals(other as PollAnswer);
        }

        public override int GetHashCode()
        {
            return (Answer, AnswerEmoji).GetHashCode();
        }
    }
}