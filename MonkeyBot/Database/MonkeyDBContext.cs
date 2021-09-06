using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MonkeyBot.Models;
using NLog.Extensions.Logging;
using System;
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
        public DbSet<RoleButtonLink> RoleButtonLinks { get; set; }
        public DbSet<Poll> Polls { get; set; }

        public MonkeyDBContext() : base()
        {
        }

        public MonkeyDBContext(DbContextOptions<MonkeyDBContext> options) : base(options)
        {
        }

        private static readonly ILoggerFactory NLogLoggerFactory = LoggerFactory.Create(builder =>
        {
            builder
            .AddFilter((category, level) => level == LogLevel.Warning)
            .AddNLog();
        });

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            string databasePath = Path.Combine(AppContext.BaseDirectory, "Data");
            if (!Directory.Exists(databasePath))
            {
                Directory.CreateDirectory(databasePath);
            }
            string datadir = Path.Combine(databasePath, "MonkeyDatabase.sqlite.db");
            optionsBuilder.UseLoggerFactory(NLogLoggerFactory);
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
            modelBuilder.Entity<GuildConfig>().Property(x => x.Rules).HasJsonConversion();
            modelBuilder.Entity<GuildConfig>().Property(x => x.BattlefieldUpdatesEnabled).HasDefaultValue(false);
            modelBuilder.Entity<GuildConfig>().Property(x => x.ConfirmedStreamerIds).HasJsonConversion();
            modelBuilder.Entity<GuildConfig>().Property(x => x.StreamAnnouncementsEnabled).HasDefaultValue(false);

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
                    x => x.ToString(),
                    x => System.Net.IPEndPoint.Parse(x));

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

            //Polls
            modelBuilder.Entity<Poll>().HasKey(x => x.Id);
            modelBuilder.Entity<Poll>().Property(x => x.GuildId).IsRequired();
            modelBuilder.Entity<Poll>().Property(x => x.ChannelId).IsRequired();
            modelBuilder.Entity<Poll>().Property(x => x.MessageId).IsRequired();
            modelBuilder.Entity<Poll>().Property(x => x.CreatorId).IsRequired();
            modelBuilder.Entity<Poll>().Property(x => x.Question).IsRequired();
            modelBuilder.Entity<Poll>().Property(x => x.PossibleAnswers).IsRequired().HasJsonConversion();
            modelBuilder.Entity<Poll>().Property(x => x.EndTimeUTC).IsRequired();
        }
    }
}