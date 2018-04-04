using System.ComponentModel.DataAnnotations;

namespace MonkeyBot.Database.Entities
{
    public class TriviaScoreEntity : BaseGuildEntity
    {
        [Required]
        public ulong UserId { get; set; }

        [Required]
        public int Score { get; set; }

        public TriviaScoreEntity()
        {
        }

        public TriviaScoreEntity(ulong guildId, ulong userId, int score)
        {
            GuildId = guildId;
            UserId = userId;
            Score = score;
        }
    }
}