using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace MonkeyBot.Migrations
{
    public partial class UpdatedReminders : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.RenameTable(
                name: "Announcements",
                newName: "Reminder");

            migrationBuilder.RenameColumn(
                table: "Reminder",
                name: "ID",
                newName: "Id");

            migrationBuilder.RenameColumn(
                table: "Reminder",
                name: "GuildID",
                newName: "GuildId");

            migrationBuilder.RenameColumn(
                table: "Reminder",
                name: "ChannelID",
                newName: "ChannelId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.RenameTable(
                name: "Reminder",
                newName: "Announcements");

            migrationBuilder.RenameColumn(
                table: "Reminder",
                name: "Id",
                newName: "ID");

            migrationBuilder.RenameColumn(
                table: "Reminder",
                name: "GuildId",
                newName: "GuildID");

            migrationBuilder.RenameColumn(
                table: "Reminder",
                name: "ChannelId",
                newName: "ChannelID");
        }
    }
}
