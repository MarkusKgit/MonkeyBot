using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace MonkeyBot.Migrations
{
    public partial class Feeds : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "New_GuildConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CommandPrefix = table.Column<string>(nullable: false),
                    GuildId = table.Column<ulong>(nullable: false),
                    Rules = table.Column<string>(nullable: true),
                    WelcomeMessageText = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildConfigs", x => x.Id);
                });

            migrationBuilder.Sql("INSERT INTO New_GuildConfigs SELECT Id, CommandPrefix, GuildId, Rules, WelcomeMessageText FROM GuildConfigs;");
            migrationBuilder.Sql("PRAGMA foreign_keys=\"0\"", true);
            migrationBuilder.Sql("DROP TABLE GuildConfigs", true);
            migrationBuilder.Sql("ALTER TABLE New_GuildConfigs RENAME TO GuildConfigs", true);
            migrationBuilder.Sql("PRAGMA foreign_keys=\"1\"", true);

            migrationBuilder.CreateTable(
                name: "Feeds",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChannelId = table.Column<ulong>(nullable: false),
                    GuildId = table.Column<ulong>(nullable: false),
                    LastUpdate = table.Column<DateTime>(nullable: true),
                    URL = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Feeds", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Feeds");

            migrationBuilder.AddColumn<long>(
                name: "FeedChannelId",
                table: "GuildConfigs",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "FeedUrls",
                table: "GuildConfigs",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ListenToFeeds",
                table: "GuildConfigs",
                nullable: false,
                defaultValue: false);
        }
    }
}