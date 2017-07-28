using MonkeyBot.Database.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MonkeyBot.Database.Repositories
{
    public interface ITriviaScoresRepository : IRepository<TriviaScoreEntity>
    {
        Task<List<TriviaScoreEntity>> GetGuildScoresAsync(ulong guildID);

        Task<TriviaScoreEntity> GetGuildUserScoreAsync(ulong guildID, ulong userID);

        Task IncreaseScoreAsync(ulong guildID, ulong userID);
    }
}