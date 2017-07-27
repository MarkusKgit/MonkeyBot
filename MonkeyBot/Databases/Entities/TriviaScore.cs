using System.ComponentModel.DataAnnotations;

namespace MonkeyBot.Databases.Entities
{
    public class TriviaScore
    {
        [Key]
        public int ID { get; set; }

        [Required]
        public ulong GuildID { get; set; }

        [Required]
        public ulong UserID { get; set; }

        public int Score { get; set; }

        public TriviaScore() { }

        public TriviaScore(ulong guildID, ulong userID, int score)
        {
            GuildID = guildID;
            UserID = userID;
            Score = score;
        }
    }
}
