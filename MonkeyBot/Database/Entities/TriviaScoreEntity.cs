using System.ComponentModel.DataAnnotations;

namespace MonkeyBot.Database.Entities
{
    public class TriviaScoreEntity : BaseEntity
    {
        [Required]
        public ulong GuildId { get; set; }

        [Required]
        public ulong UserId { get; set; }

        [Required]
        public int Score { get; set; }

        public TriviaScoreEntity()
        {
        }

        public TriviaScoreEntity(ulong guildID, ulong userID, int score)
        {
            GuildId = guildID;
            UserId = userID;
            Score = score;
        }
    }
}