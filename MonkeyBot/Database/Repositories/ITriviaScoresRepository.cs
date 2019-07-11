using MonkeyBot.Database.Entities;
using MonkeyBot.Services;
using System.Threading.Tasks;

namespace MonkeyBot.Database.Repositories
{
    public interface ITriviaScoresRepository : IGuildRepository<TriviaScoreEntity, TriviaScore>
    {
        Task<TriviaScore> GetGuildUserScoreAsync(ulong guildID, ulong userID);

        Task IncreaseScoreAsync(ulong guildID, ulong userID, int points);
    }
}