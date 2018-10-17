using Microsoft.EntityFrameworkCore.Migrations;

namespace MonkeyBot.Migrations
{
    public partial class GameServer : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GameServerType",
                table: "GameServers",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GameServerType",
                table: "GameServers");
        }
    }
}
