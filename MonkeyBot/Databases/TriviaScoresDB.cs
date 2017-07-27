using Discord;
using Microsoft.EntityFrameworkCore;
using MonkeyBot.Databases.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        /// <summary>
        /// Returns a formated string that contains the specified amount of high scores in the specified guild
        /// </summary>
        /// <param name="client">DiscordClient instance</param>
        /// <param name="count">max number of high scores to get</param>
        /// <param name="guildID">Id of the Discord Guild</param>
        /// <returns></returns>
        public async Task<string> GetAllTimeHighScoresAsync(IDiscordClient client, int count, ulong guildID)
        {
            var userScoresAllTime = await TriviaScores.Where(x => x.GuildID == guildID).ToListAsync();
            int correctedCount = Math.Min(count, userScoresAllTime.Count);
            if (correctedCount < 1)
                return "No scores found!";
            var sortedScores = userScoresAllTime.OrderByDescending(x => x.Score);
            sortedScores.Take(correctedCount);
            List<string> scoresList = new List<string>();
            foreach (var score in sortedScores)
            {
                var userName = (await client.GetUserAsync(score.UserID)).Username;
                if (score.Score == 1)
                    scoresList.Add($"{userName}: 1 point");
                else
                    scoresList.Add($"{userName}: {score.Score} points");
            }
            string scores = $"**Top {correctedCount} of all time**:{Environment.NewLine}{string.Join(", ", scoresList)}";
            return scores;
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
