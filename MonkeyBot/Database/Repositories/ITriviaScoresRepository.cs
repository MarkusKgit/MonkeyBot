using MonkeyBot.Database.Entities;
using MonkeyBot.Services.Common.Trivia;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MonkeyBot.Database.Repositories
{
    public interface ITriviaScoresRepository : IRepository<TriviaScoreEntity, TriviaScore>
    {
        Task<List<TriviaScore>> GetGuildScoresAsync(ulong guildID);

        Task<TriviaScore> GetGuildUserScoreAsync(ulong guildID, ulong userID);

        Task IncreaseScoreAsync(ulong guildID, ulong userID, int points);
    }
}