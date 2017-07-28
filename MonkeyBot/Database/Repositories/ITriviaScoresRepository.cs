using MonkeyBot.Database.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MonkeyBot.Database.Repositories
{
    public interface ITriviaScoresRepository : IRepository<TriviaScore>
    {
        Task<List<TriviaScore>> GetGuildScoresAsync(ulong guildID);

        Task<TriviaScore> GetGuildUserScoreAsync(ulong guildID, ulong userID);

        Task IncreaseScoreAsync(ulong guildID, ulong userID);
    }
}