using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace MonkeyBot.Migrations
{
    public partial class GiveAways : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<ulong>(
                name: "GiveAwayChannel",
                table: "GuildConfigs",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0ul);

            migrationBuilder.AddColumn<bool>(
                name: "GiveAwaysEnabled",
                table: "GuildConfigs",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastGiveAway",
                table: "GuildConfigs",
                type: "TEXT",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GiveAwayChannel",
                table: "GuildConfigs");

            migrationBuilder.DropColumn(
                name: "GiveAwaysEnabled",
                table: "GuildConfigs");

            migrationBuilder.DropColumn(
                name: "LastGiveAway",
                table: "GuildConfigs");
        }
    }
}
