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
                await dbSet.AddAsync(dbScore = new TriviaScoreEntity());
            dbScore.GuildId = tvs.GuildID;
            dbScore.UserId = tvs.UserID;
            dbScore.Score = tvs.Score;
            dbSet.Update(dbScore);
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
            await IncreaseScoreAsync(score);
        }

        private async Task IncreaseScoreAsync(TriviaScoreEntity score)
        {            
            var ts = await dbSet.FirstOrDefaultAsync(x => x.Id == score.Id);
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