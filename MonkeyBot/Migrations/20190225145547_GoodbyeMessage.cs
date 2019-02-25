using Microsoft.EntityFrameworkCore.Migrations;

namespace MonkeyBot.Migrations
{
    public partial class GoodbyeMessage : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<ulong>(
                name: "GoodbyeMessageChannelId",
                table: "GuildConfigs",
                nullable: false,
                defaultValue: 0ul);

            migrationBuilder.AddColumn<string>(
                name: "GoodbyeMessageText",
                table: "GuildConfigs",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GoodbyeMessageChannelId",
                table: "GuildConfigs");

            migrationBuilder.DropColumn(
                name: "GoodbyeMessageText",
                table: "GuildConfigs");
        }
    }
}
