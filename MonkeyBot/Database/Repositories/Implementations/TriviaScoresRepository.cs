using Microsoft.EntityFrameworkCore;
using MonkeyBot.Database.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MonkeyBot.Database.Repositories
{
    public class TriviaScoresRepository : BaseRepository<TriviaScore>, ITriviaScoresRepository
    {
        public TriviaScoresRepository(DbContext context) : base(context)
        {
        }

        public Task<List<TriviaScore>> GetGuildScoresAsync(ulong guildID)
        {
            return dbSet.Where(x => x.GuildID == guildID).ToListAsync();
        }

        public Task<TriviaScore> GetGuildUserScoreAsync(ulong guildID, ulong userID)
        {
            return dbSet.FirstOrDefaultAsync(x => x.GuildID == guildID && x.UserID == userID);
        }

        public async Task IncreaseScoreAsync(ulong guildID, ulong userID)
        {
            var score = await dbSet.FirstOrDefaultAsync(x => x.GuildID == guildID && x.UserID == userID);
            await IncreaseScoreAsync(score);
        }

        public async Task IncreaseScoreAsync(TriviaScore score)
        {
            var ts = await dbSet.FirstOrDefaultAsync(x => x == score);
            if (ts == null)
                await dbSet.AddAsync(ts = new TriviaScore(score.GuildID, score.UserID, 1));
            else
                ts.Score++;
            await context.SaveChangesAsync();
        }
    }
}