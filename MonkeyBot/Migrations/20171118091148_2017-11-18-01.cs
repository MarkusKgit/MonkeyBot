using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace MonkeyBot.Migrations
{
    public partial class _2017111801 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GameVersion",
                table: "GameServers",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastVersionUpdate",
                table: "GameServers",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GameVersion",
                table: "GameServers");

            migrationBuilder.DropColumn(
                name: "LastVersionUpdate",
                table: "GameServers");
        }
    }
}
