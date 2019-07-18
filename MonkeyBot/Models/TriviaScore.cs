using System.ComponentModel.DataAnnotations;

namespace MonkeyBot.Models
{
    public class TriviaScore
    {
        public int ID { get; set; }

        public ulong GuildID { get; set; }

        public ulong UserID { get; set; }

        public int Score { get; set; }
    }
}