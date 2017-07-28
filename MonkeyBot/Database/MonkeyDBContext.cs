using Microsoft.EntityFrameworkCore;
using MonkeyBot.Database.Entities;
using System;
using System.IO;

namespace MonkeyBot.Database
{
    public class MonkeyDBContext : DbContext
    {
        public DbSet<TriviaScore> TriviaScores { get; set; }

        public MonkeyDBContext() : base()
        {
            Database.EnsureCreated();
        }

        public MonkeyDBContext(DbContextOptions options) : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            string databasePath = Path.Combine(AppContext.BaseDirectory, "Data");
            if (!Directory.Exists(databasePath))
                Directory.CreateDirectory(databasePath);
            string datadir = Path.Combine(databasePath, "MonkeyDatabase.sqlite.db");
            optionsBuilder.UseSqlite($"Filename={datadir}");
        }

        public void EnsureSeedData()
        {
        }
    }
}