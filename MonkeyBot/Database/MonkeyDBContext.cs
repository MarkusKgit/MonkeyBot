using Microsoft.EntityFrameworkCore;
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
        public DbSet<TriviaScore> TriviaScores { get; set; }
        public DbSet<Announcement> Announcements { get; set; }
        public DbSet<Feed> Feeds { get; set; }
        public DbSet<GameServer> GameServers { get; set; }
        public DbSet<GameSubscription> GameSubscriptions { get; set; }
        public DbSet<RoleButtonLink> RoleButtonLinks { get; set; }

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
            modelBuilder.Entity<GuildConfig>().Property(x => x.BattlefieldUpdatesEnabled).HasDefaultValue(false);

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

            //GameSubscriptions
            modelBuilder.Entity<GameSubscription>().HasKey(x => x.ID);
            modelBuilder.Entity<GameSubscription>().Property(x => x.GuildID).IsRequired();
            modelBuilder.Entity<GameSubscription>().Property(x => x.UserID).IsRequired();
            modelBuilder.Entity<GameSubscription>().Property(x => x.GameName).IsRequired();

            //TriviaScores
            modelBuilder.Entity<TriviaScore>().HasKey(x => x.ID);
            modelBuilder.Entity<TriviaScore>().Property(x => x.GuildID).IsRequired();
            modelBuilder.Entity<TriviaScore>().Property(x => x.UserID).IsRequired();
            modelBuilder.Entity<TriviaScore>().Property(x => x.Score).IsRequired();

            //RoleButtonlinks
            modelBuilder.Entity<RoleButtonLink>().HasKey(x => x.ID);
            modelBuilder.Entity<RoleButtonLink>().Property(x => x.GuildID).IsRequired();
            modelBuilder.Entity<RoleButtonLink>().Property(x => x.RoleID).IsRequired();
            modelBuilder.Entity<RoleButtonLink>().Property(x => x.EmoteString).IsRequired();
            modelBuilder.Entity<RoleButtonLink>().Property(x => x.MessageID).IsRequired();

            //RoleButtonlinks
            modelBuilder.Entity<Announcement>().HasKey(x => x.ID);
            modelBuilder.Entity<Announcement>().Property(x => x.GuildID).IsRequired();
            modelBuilder.Entity<Announcement>().Property(x => x.ChannelID).IsRequired();
            modelBuilder.Entity<Announcement>().Property(x => x.Type).IsRequired().HasConversion<string>();
            modelBuilder.Entity<Announcement>().Property(x => x.Message).IsRequired();
            modelBuilder.Entity<Announcement>().Property(x => x.Name).IsRequired();
        }
    }
}