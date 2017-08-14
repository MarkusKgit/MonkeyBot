using Microsoft.EntityFrameworkCore.Migrations;

namespace MonkeyBot.Migrations
{
    public partial class _20170811 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FeedUrls",
                table: "GuildConfigs");

            migrationBuilder.DropColumn(
                name: "ListenToFeeds",
                table: "GuildConfigs");
        }
    }
}