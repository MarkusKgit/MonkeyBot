using Microsoft.EntityFrameworkCore.Migrations;

namespace MonkeyBot.Migrations
{
    public partial class StreamingAnnouncements : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ConfirmedStreamerIds",
                table: "GuildConfigs",
                nullable: true);

            migrationBuilder.AddColumn<ulong>(
                name: "DefaultChannelId",
                table: "GuildConfigs",
                nullable: false,
                defaultValue: 0ul);

            migrationBuilder.AddColumn<bool>(
                name: "StreamAnnouncementsEnabled",
                table: "GuildConfigs",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConfirmedStreamerIds",
                table: "GuildConfigs");

            migrationBuilder.DropColumn(
                name: "DefaultChannelId",
                table: "GuildConfigs");

            migrationBuilder.DropColumn(
                name: "StreamAnnouncementsEnabled",
                table: "GuildConfigs");
        }
    }
}
