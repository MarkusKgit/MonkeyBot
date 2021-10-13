using Microsoft.EntityFrameworkCore.Migrations;

namespace MonkeyBot.Migrations
{
    public partial class RemoveReactionRoleAssignment : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RoleButtonLinks");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RoleButtonLinks",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChannelID = table.Column<ulong>(type: "INTEGER", nullable: false),
                    EmoteString = table.Column<string>(type: "TEXT", nullable: false),
                    GuildID = table.Column<ulong>(type: "INTEGER", nullable: false),
                    MessageID = table.Column<ulong>(type: "INTEGER", nullable: false),
                    RoleID = table.Column<ulong>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleButtonLinks", x => x.ID);
                });
        }
    }
}
