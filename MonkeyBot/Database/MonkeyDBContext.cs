using Microsoft.EntityFrameworkCore;
using MonkeyBot.Database.Entities;
using MonkeyBot.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace MonkeyBot.Database
{
    public class MonkeyDBContext : DbContext
    {
        public DbSet<BenzenFact> BenzenFacts { get; set; }

        public DbSet<GuildConfig> GuildConfigs { get; set; }
        public DbSet<TriviaScoreEntity> TriviaScores { get; set; }
        public DbSet<AnnouncementEntity> Announcements { get; set; }
        public DbSet<Feed> Feeds { get; set; }
        public DbSet<GameServer> GameServers { get; set; }
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
            //BenzenFacts
            modelBuilder.Entity<BenzenFact>().HasKey(x => x.ID);
            modelBuilder.Entity<BenzenFact>().Property(x => x.Fact).IsRequired();

            //GuildConfigs
            modelBuilder.Entity<GuildConfig>().HasKey(x => x.ID);
            modelBuilder.Entity<GuildConfig>().Property(x => x.GuildID).IsRequired();
            modelBuilder.Entity<GuildConfig>().Property(x => x.CommandPrefix).IsRequired();
            modelBuilder.Entity<GuildConfig>().Property(x => x.Rules)
                .HasConversion(
                    x => JsonConvert.SerializeObject(x),
                    x => JsonConvert.DeserializeObject<List<string>>(x));

            //Feeds
            modelBuilder.Entity<Feed>().HasKey(x => x.ID);
            modelBuilder.Entity<Feed>().Property(x => x.GuildID).IsRequired();
            modelBuilder.Entity<Feed>().Property(x => x.ChannelID).IsRequired();
            modelBuilder.Entity<Feed>().Property(x => x.Name).IsRequired();
            modelBuilder.Entity<Feed>().Property(x => x.URL).IsRequired();

            //GameServers
            modelBuilder.Entity<GameServer>().HasKey(x => x.ID);
            modelBuilder.Entity<GameServer>().Property(x => x.GuildID).IsRequired();
            modelBuilder.Entity<GameServer>().Property(x => x.ChannelID).IsRequired();
            modelBuilder.Entity<GameServer>().Property(x => x.GameServerType).IsRequired().HasConversion<string>();
            modelBuilder.Entity<GameServer>().Property(x => x.ServerIP).IsRequired()
                .HasConversion(
                    x => JsonConvert.SerializeObject(x),
                    x => JsonConvert.DeserializeObject<System.Net.IPEndPoint>(x));
        }
    }
}