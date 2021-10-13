using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace MonkeyBot.Migrations
{
    public partial class MessageComponentLinkUpdate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "MessageID",
                table: "MessageComponentLinks",
                newName: "MessageId");

            migrationBuilder.RenameColumn(
                name: "GuildID",
                table: "MessageComponentLinks",
                newName: "GuildId");

            migrationBuilder.RenameColumn(
                name: "ChannelID",
                table: "MessageComponentLinks",
                newName: "ChannelId");

            migrationBuilder.RenameColumn(
                name: "ID",
                table: "MessageComponentLinks",
                newName: "Id");

            migrationBuilder.AddColumn<ulong>(
                name: "ParentMessageId",
                table: "MessageComponentLinks",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0ul);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ParentMessageId",
                table: "MessageComponentLinks");

            migrationBuilder.RenameColumn(
                name: "MessageId",
                table: "MessageComponentLinks",
                newName: "MessageID");

            migrationBuilder.RenameColumn(
                name: "GuildId",
                table: "MessageComponentLinks",
                newName: "GuildID");

            migrationBuilder.RenameColumn(
                name: "ChannelId",
                table: "MessageComponentLinks",
                newName: "ChannelID");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "MessageComponentLinks",
                newName: "ID");
        }
    }
}
