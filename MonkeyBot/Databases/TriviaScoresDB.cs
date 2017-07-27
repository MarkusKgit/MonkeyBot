using Microsoft.EntityFrameworkCore;
using MonkeyBot.Databases.Entities;
using System;
using System.IO;
using System.Threading.Tasks;

namespace MonkeyBot.Databases
{
    public class TriviaScoresDB : DbContext
    {
        public DbSet<TriviaScore> TriviaScores { get; set; }

        public TriviaScoresDB()
        {
            Database.EnsureCreated();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            string triviaPath = Path.Combine(AppContext.BaseDirectory, "TriviaScores");
            if (!Directory.Exists("triviaPath"))
                Directory.CreateDirectory(triviaPath);
            string datadir = Path.Combine(triviaPath, "TriviaScores.sqlite.db");
            optionsBuilder.UseSqlite($"Filename={datadir}");
        }

        public async Task<TriviaScore> GetScoreAsync(ulong guildID, ulong userID)
        {
            var score = await TriviaScores.FirstOrDefaultAsync(x => x.GuildID == guildID && x.UserID == userID);

            if (score != null)
                return score;

            score = new TriviaScore(guildID, userID, 0);
            await TriviaScores.AddAsync(score);
            await SaveChangesAsync();
            return score;
        }

        public async Task IncreaseScoreAsync(ulong guildID, ulong userID)
        {
            var score = await GetScoreAsync(guildID, userID);
            await IncreaseScoreAsync(score);
        }

        public async Task IncreaseScoreAsync(TriviaScore score)
        {
            score.Score++;
            TriviaScores.Update(score);
            await SaveChangesAsync();
        }
    }
}
