using Microsoft.EntityFrameworkCore;
using MonkeyBot.Database.Entities;
using System;
using System.IO;
using System.Linq;

namespace MonkeyBot.Database
{
    public class MonkeyDBContext : DbContext
    {
        public DbSet<BenzenFact> BenzenFacts { get; set; }

        public DbSet<GuildConfigEntity> GuildConfigs { get; set; }
        public DbSet<TriviaScoreEntity> TriviaScores { get; set; }
        public DbSet<AnnouncementEntity> Announcements { get; set; }
        public DbSet<FeedEntity> Feeds { get; set; }
        public DbSet<GameServerEntity> GameServers { get; set; }
        public DbSet<GameSubscriptionEntity> GameSubscriptions { get; set; }
        public DbSet<RoleButtonLinkEntity> RoleButtonLinks { get; set; }

        public MonkeyDBContext() : base()
        {
        }

        public MonkeyDBContext(DbContextOptions<MonkeyDBContext> options) : base(options)
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
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<BenzenFact>().HasKey(x => x.ID);
            modelBuilder.Entity<BenzenFact>().Property(x => x.Fact).IsRequired();            
        }
    }
}