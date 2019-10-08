using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace MonkeyBot.Migrations
{
    public partial class BattlefieldUpdates : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<ulong>(
                name: "BattlefieldUpdatesChannel",
                table: "GuildConfigs",
                nullable: false,
                defaultValue: 0ul);

            migrationBuilder.AddColumn<bool>(
                name: "BattlefieldUpdatesEnabled",
                table: "GuildConfigs",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastBattlefieldUpdate",
                table: "GuildConfigs",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BattlefieldUpdatesChannel",
                table: "GuildConfigs");

            migrationBuilder.DropColumn(
                name: "BattlefieldUpdatesEnabled",
                table: "GuildConfigs");

            migrationBuilder.DropColumn(
                name: "LastBattlefieldUpdate",
                table: "GuildConfigs");
        }
    }
}
