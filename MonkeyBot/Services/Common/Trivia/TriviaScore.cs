namespace MonkeyBot.Services.Common.Trivia
{
    public class TriviaScore
    {
        public ulong GuildID { get; set; }

        public ulong UserID { get; set; }

        public int Score { get; set; }
    }
}