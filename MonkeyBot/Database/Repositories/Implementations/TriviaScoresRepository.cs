using Microsoft.EntityFrameworkCore;
using MonkeyBot.Database.Entities;
using MonkeyBot.Services.Common.Trivia;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
using AutoMapper;

namespace MonkeyBot.Database.Repositories
{
    public class TriviaScoresRepository : BaseRepository<TriviaScoreEntity, TriviaScore>, ITriviaScoresRepository
    {
        public TriviaScoresRepository(DbContext context) : base(context)
        {
        }

        public override async Task AddOrUpdateAsync(TriviaScore tvs)
        {
            var dbScore = await dbSet.FirstOrDefaultAsync(x => x.GuildId == tvs.GuildID && x.UserId == tvs.UserID);
            if (dbScore == null)
            {
                await dbSet.AddAsync(dbScore = new TriviaScoreEntity()
                {
                    GuildId = tvs.GuildID,
                    UserId = tvs.UserID,
                    Score = tvs.Score
                });
            }
            else
            {
                dbScore.GuildId = tvs.GuildID;
                dbScore.UserId = tvs.UserID;
                dbScore.Score = tvs.Score;
                dbSet.Update(dbScore);
            }
        }
        
        public Task<List<TriviaScore>> GetGuildScoresAsync(ulong guildID)
        {            
            return dbSet.Where(x => x.GuildId == guildID).Select(x => Mapper.Map<TriviaScore>(x)).ToListAsync();            
        }

        public Task<TriviaScore> GetGuildUserScoreAsync(ulong guildID, ulong userID)
        {
            return dbSet.Where(x => x.GuildId == guildID && x.UserId == userID).Select(x => Mapper.Map<TriviaScore>(x)).FirstOrDefaultAsync();            
        }

        public async Task IncreaseScoreAsync(ulong guildID, ulong userID)
        {
            var score = await dbSet.FirstOrDefaultAsync(x => x.GuildId == guildID && x.UserId == userID);
            if (score != null)
                await IncreaseScoreAsync(score);
            else
                await dbSet.AddAsync(new TriviaScoreEntity(guildID, userID, 1));
        }

        private async Task IncreaseScoreAsync(TriviaScoreEntity score)
        {
            TriviaScoreEntity ts = await dbSet.FirstOrDefaultAsync(x => x.Id == score.Id);
            if (ts == null)
                await dbSet.AddAsync(ts = new TriviaScoreEntity(score.GuildId, score.UserId, 1));
            else
            {
                ts.Score++;
                dbSet.Update(ts);
            }
        }
    }
}