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
                newName: "Reminders");

            migrationBuilder.RenameColumn(
                table: "Reminders",
                name: "ID",
                newName: "Id");

            migrationBuilder.RenameColumn(
                table: "Reminders",
                name: "GuildID",
                newName: "GuildId");

            migrationBuilder.RenameColumn(
                table: "Reminders",
                name: "ChannelID",
                newName: "ChannelId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                table: "Reminders",
                name: "Id",
                newName: "ID");

            migrationBuilder.RenameColumn(
                table: "Reminders",
                name: "GuildId",
                newName: "GuildID");

            migrationBuilder.RenameColumn(
                table: "Reminders",
                name: "ChannelId",
                newName: "ChannelID");

            migrationBuilder.RenameTable(
                name: "Reminders",
                newName: "Announcements");
        }
    }
}
