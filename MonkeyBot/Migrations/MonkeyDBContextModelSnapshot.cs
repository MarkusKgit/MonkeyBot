using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using MonkeyBot.Database;
using System;

namespace MonkeyBot.Migrations
{
    [DbContext(typeof(MonkeyDBContext))]
    partial class MonkeyDBContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
            modelBuilder
                .HasAnnotation("ProductVersion", "1.1.2");

            modelBuilder.Entity("MonkeyBot.Database.Entities.AnnouncementEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<ulong>("ChannelId");

                    b.Property<string>("CronExpression");

                    b.Property<DateTime?>("ExecutionTime");

                    b.Property<ulong>("GuildId");

                    b.Property<string>("Message")
                        .IsRequired();

                    b.Property<string>("Name")
                        .IsRequired();

                    b.Property<int>("Type");

                    b.HasKey("Id");

                    b.ToTable("Announcements");
                });

            modelBuilder.Entity("MonkeyBot.Database.Entities.BenzenFactEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Fact");

                    b.HasKey("Id");

                    b.ToTable("BenzenFacts");
                });

            modelBuilder.Entity("MonkeyBot.Database.Entities.GameServerEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<long>("ChannelId");

                    b.Property<string>("GameVersion");

                    b.Property<long>("GuildId");

                    b.Property<string>("IPAsString")
                        .IsRequired()
                        .HasColumnName("IP");

                    b.Property<DateTime?>("LastVersionUpdate");

                    b.Property<long?>("MessageId");

                    b.HasKey("Id");

                    b.ToTable("GameServers");
                });

            modelBuilder.Entity("MonkeyBot.Database.Entities.GameSubscriptionEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("GameName")
                        .IsRequired();

                    b.Property<long>("GuildId");

                    b.Property<long>("UserId");

                    b.HasKey("Id");

                    b.ToTable("GameSubscriptions");
                });

            modelBuilder.Entity("MonkeyBot.Database.Entities.GuildConfigEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("CommandPrefix")
                        .IsRequired();

                    b.Property<long>("FeedChannelId");

                    b.Property<string>("FeedUrlsAsString")
                        .HasColumnName("FeedUrls");

                    b.Property<long>("GuildId");

                    b.Property<bool>("ListenToFeeds");

                    b.Property<string>("RulesAsString")
                        .HasColumnName("Rules");

                    b.Property<string>("WelcomeMessageText");

                    b.HasKey("Id");

                    b.ToTable("GuildConfigs");
                });

            modelBuilder.Entity("MonkeyBot.Database.Entities.TriviaScoreEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<ulong>("GuildID");

                    b.Property<int>("Score");

                    b.Property<ulong>("UserID");

                    b.HasKey("Id");

                    b.ToTable("TriviaScores");
                });
        }
    }
}