using AutoMapper;
using Microsoft.EntityFrameworkCore;
using MonkeyBot.Database.Entities;
using MonkeyBot.Services.Common.Trivia;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MonkeyBot.Database.Repositories
{
    public class TriviaScoresRepository : BaseRepository<TriviaScoreEntity, TriviaScore>, ITriviaScoresRepository
    {
        public TriviaScoresRepository(DbContext context) : base(context)
        {
        }

        public override async Task AddOrUpdateAsync(TriviaScore tvs)
        {
            var dbScore = await dbSet.FirstOrDefaultAsync(x => x.GuildID == tvs.GuildID && x.UserID == tvs.UserID);
            if (dbScore == null)
            {
                await dbSet.AddAsync(dbScore = new TriviaScoreEntity()
                {
                    GuildID = tvs.GuildID,
                    UserID = tvs.UserID,
                    Score = tvs.Score
                });
            }
            else
            {
                dbScore.GuildID = tvs.GuildID;
                dbScore.UserID = tvs.UserID;
                dbScore.Score = tvs.Score;
                dbSet.Update(dbScore);
            }
        }

        public Task<List<TriviaScore>> GetGuildScoresAsync(ulong guildID)
        {
            return dbSet.Where(x => x.GuildID == guildID).Select(x => Mapper.Map<TriviaScore>(x)).ToListAsync();
        }

        public Task<TriviaScore> GetGuildUserScoreAsync(ulong guildID, ulong userID)
        {
            return dbSet.Where(x => x.GuildID == guildID && x.UserID == userID).Select(x => Mapper.Map<TriviaScore>(x)).FirstOrDefaultAsync();
        }

        public async Task IncreaseScoreAsync(ulong guildID, ulong userID, int points)
        {
            var score = await dbSet.FirstOrDefaultAsync(x => x.GuildID == guildID && x.UserID == userID);
            if (score != null)
            {
                score.Score += points;
                dbSet.Update(score);
            }
            else
                await dbSet.AddAsync(new TriviaScoreEntity(guildID, userID, points));
        }

        public override async Task RemoveAsync(TriviaScore obj)
        {
            if (obj == null)
                return;
            var entity = await dbSet.FirstOrDefaultAsync(x => x.GuildID == obj.GuildID && x.UserID == obj.UserID);
            if (entity != null)
                dbSet.Remove(entity);
        }
    }
}