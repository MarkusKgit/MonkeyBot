using System.ComponentModel.DataAnnotations;

namespace MonkeyBot.Database.Entities
{
    public class TriviaScoreEntity : BaseEntity
    {
        [Required]
        public ulong GuildID { get; set; }

        [Required]
        public ulong UserID { get; set; }

        [Required]
        public int Score { get; set; }

        public TriviaScoreEntity()
        {
        }

        public TriviaScoreEntity(ulong guildID, ulong userID, int score)
        {
            GuildID = guildID;
            UserID = userID;
            Score = score;
        }
    }
}