using Microsoft.EntityFrameworkCore.Migrations;

namespace MonkeyBot.Migrations
{
    public partial class _201708112 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "FeedChannelId",
                table: "GuildConfigs",
                nullable: false,
                defaultValue: 0L);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FeedChannelId",
                table: "GuildConfigs");
        }
    }
}