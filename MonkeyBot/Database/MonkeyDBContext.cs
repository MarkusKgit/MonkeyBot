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
            {
                _ = Directory.CreateDirectory(databasePath);
            }
            string datadir = Path.Combine(databasePath, "MonkeyDatabase.sqlite.db");
            _ = optionsBuilder.UseSqlite($"Filename={datadir}");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //BenzenFacts
            _ = modelBuilder.Entity<BenzenFact>().HasKey(x => x.ID);
            _ = modelBuilder.Entity<BenzenFact>().Property(x => x.Fact).IsRequired();

            //GuildConfigs
            _ = modelBuilder.Entity<GuildConfig>().HasKey(x => x.ID);
            _ = modelBuilder.Entity<GuildConfig>().Property(x => x.GuildID).IsRequired();
            _ = modelBuilder.Entity<GuildConfig>().Property(x => x.CommandPrefix).IsRequired();
            _ = modelBuilder.Entity<GuildConfig>().Property(x => x.Rules)
                .HasConversion(
                    x => JsonConvert.SerializeObject(x),
                    x => JsonConvert.DeserializeObject<List<string>>(x));
            _ = modelBuilder.Entity<GuildConfig>().Property(x => x.BattlefieldUpdatesEnabled).HasDefaultValue(false);
            _ = modelBuilder.Entity<GuildConfig>().Property(x => x.ConfirmedStreamerIds)
                .HasConversion(
                    x => JsonConvert.SerializeObject(x),
                    x => JsonConvert.DeserializeObject<List<ulong>>(x));
            _ = modelBuilder.Entity<GuildConfig>().Property(x => x.StreamAnnouncementsEnabled).HasDefaultValue(false);

            //Feeds
            _ = modelBuilder.Entity<Feed>().HasKey(x => x.ID);
            _ = modelBuilder.Entity<Feed>().Property(x => x.GuildID).IsRequired();
            _ = modelBuilder.Entity<Feed>().Property(x => x.ChannelID).IsRequired();
            _ = modelBuilder.Entity<Feed>().Property(x => x.Name).IsRequired();
            _ = modelBuilder.Entity<Feed>().Property(x => x.URL).IsRequired();

            //GameServers
            _ = modelBuilder.Entity<GameServer>().HasKey(x => x.ID);
            _ = modelBuilder.Entity<GameServer>().Property(x => x.GuildID).IsRequired();
            _ = modelBuilder.Entity<GameServer>().Property(x => x.ChannelID).IsRequired();
            _ = modelBuilder.Entity<GameServer>().Property(x => x.GameServerType).IsRequired().HasConversion<string>();
            _ = modelBuilder.Entity<GameServer>().Property(x => x.ServerIP).IsRequired()
                .HasConversion(
                    x => JsonConvert.SerializeObject(x),
                    x => JsonConvert.DeserializeObject<System.Net.IPEndPoint>(x));

            //GameSubscriptions
            _ = modelBuilder.Entity<GameSubscription>().HasKey(x => x.ID);
            _ = modelBuilder.Entity<GameSubscription>().Property(x => x.GuildID).IsRequired();
            _ = modelBuilder.Entity<GameSubscription>().Property(x => x.UserID).IsRequired();
            _ = modelBuilder.Entity<GameSubscription>().Property(x => x.GameName).IsRequired();

            //TriviaScores
            _ = modelBuilder.Entity<TriviaScore>().HasKey(x => x.ID);
            _ = modelBuilder.Entity<TriviaScore>().Property(x => x.GuildID).IsRequired();
            _ = modelBuilder.Entity<TriviaScore>().Property(x => x.UserID).IsRequired();
            _ = modelBuilder.Entity<TriviaScore>().Property(x => x.Score).IsRequired();

            //RoleButtonlinks
            _ = modelBuilder.Entity<RoleButtonLink>().HasKey(x => x.ID);
            _ = modelBuilder.Entity<RoleButtonLink>().Property(x => x.GuildID).IsRequired();
            _ = modelBuilder.Entity<RoleButtonLink>().Property(x => x.RoleID).IsRequired();
            _ = modelBuilder.Entity<RoleButtonLink>().Property(x => x.EmoteString).IsRequired();
            _ = modelBuilder.Entity<RoleButtonLink>().Property(x => x.MessageID).IsRequired();

            //RoleButtonlinks
            _ = modelBuilder.Entity<Announcement>().HasKey(x => x.ID);
            _ = modelBuilder.Entity<Announcement>().Property(x => x.GuildID).IsRequired();
            _ = modelBuilder.Entity<Announcement>().Property(x => x.ChannelID).IsRequired();
            _ = modelBuilder.Entity<Announcement>().Property(x => x.Type).IsRequired().HasConversion<string>();
            _ = modelBuilder.Entity<Announcement>().Property(x => x.Message).IsRequired();
            _ = modelBuilder.Entity<Announcement>().Property(x => x.Name).IsRequired();
        }
    }
}