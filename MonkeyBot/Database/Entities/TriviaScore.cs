using System.ComponentModel.DataAnnotations;

namespace MonkeyBot.Database.Entities
{
    public class TriviaScore : BaseEntity
    {
        [Required]
        public ulong GuildID { get; set; }

        [Required]
        public ulong UserID { get; set; }

        public int Score { get; set; }

        public TriviaScore()
        {
        }

        public TriviaScore(ulong guildID, ulong userID, int score)
        {
            GuildID = guildID;
            UserID = userID;
            Score = score;
        }
    }
}