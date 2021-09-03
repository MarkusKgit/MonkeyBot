using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace MonkeyBot.Migrations
{
    public partial class Polls : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GameSubscriptions");

            migrationBuilder.CreateTable(
                name: "Polls",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildId = table.Column<ulong>(nullable: false),
                    ChannelId = table.Column<ulong>(nullable: false),
                    MessageId = table.Column<ulong>(nullable: false),
                    CreatorId = table.Column<ulong>(nullable: false),
                    Question = table.Column<string>(nullable: false),
                    PossibleAnswers = table.Column<string>(nullable: false),
                    EndTimeUTC = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Polls", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Polls");

            migrationBuilder.CreateTable(
                name: "GameSubscriptions",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GameName = table.Column<string>(type: "TEXT", nullable: false),
                    GuildID = table.Column<ulong>(type: "INTEGER", nullable: false),
                    UserID = table.Column<ulong>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameSubscriptions", x => x.ID);
                });
        }
    }
}
