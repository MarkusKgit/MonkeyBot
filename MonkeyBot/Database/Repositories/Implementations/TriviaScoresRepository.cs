using AutoMapper;
using Microsoft.EntityFrameworkCore;
using MonkeyBot.Database.Entities;
using MonkeyBot.Services;
using System.Linq;
using System.Threading.Tasks;

namespace MonkeyBot.Database.Repositories
{
    public class TriviaScoresRepository : BaseGuildRepository<TriviaScoreEntity, TriviaScore>, ITriviaScoresRepository
    {
        public TriviaScoresRepository(DbContext context) : base(context)
        {
        }

        public override async Task AddOrUpdateAsync(TriviaScore tvs)
        {
            var dbScore = await dbSet.FirstOrDefaultAsync(x => x.GuildId == tvs.GuildID && x.UserId == tvs.UserID).ConfigureAwait(false);
            if (dbScore == null)
            {
                await dbSet.AddAsync(dbScore = new TriviaScoreEntity
                {
                    GuildId = tvs.GuildID,
                    UserId = tvs.UserID,
                    Score = tvs.Score
                }).ConfigureAwait(false);
            }
            else
            {
                dbScore.GuildId = tvs.GuildID;
                dbScore.UserId = tvs.UserID;
                dbScore.Score = tvs.Score;
                dbSet.Update(dbScore);
            }
        }

        public Task<TriviaScore> GetGuildUserScoreAsync(ulong guildID, ulong userID)
        {
            return dbSet.Where(x => x.GuildId == guildID && x.UserId == userID).Select(x => Mapper.Map<TriviaScore>(x)).FirstOrDefaultAsync();
        }

        public async Task IncreaseScoreAsync(ulong guildID, ulong userID, int points)
        {
            var score = await dbSet.FirstOrDefaultAsync(x => x.GuildId == guildID && x.UserId == userID).ConfigureAwait(false);
            if (score != null)
            {
                score.Score += points;
                dbSet.Update(score);
            }
            else
                await dbSet.AddAsync(new TriviaScoreEntity(guildID, userID, points)).ConfigureAwait(false);
        }

        public override async Task RemoveAsync(TriviaScore obj)
        {
            if (obj == null)
                return;
            var entity = await dbSet.FirstOrDefaultAsync(x => x.GuildId == obj.GuildID && x.UserId == obj.UserID).ConfigureAwait(false);
            if (entity != null)
                dbSet.Remove(entity);
        }
    }
}