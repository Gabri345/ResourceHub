using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ResourceHub.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPrivateSharingAndFolders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Folder",
                table: "Resources",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsPrivate",
                table: "Resources",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "ResourceShares",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ResourceId = table.Column<int>(type: "INTEGER", nullable: false),
                    SharedWithUserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResourceShares", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResourceShares_Resources_ResourceId",
                        column: x => x.ResourceId,
                        principalTable: "Resources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ResourceShares_ResourceId_SharedWithUserId",
                table: "ResourceShares",
                columns: new[] { "ResourceId", "SharedWithUserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ResourceShares");

            migrationBuilder.DropColumn(
                name: "Folder",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "IsPrivate",
                table: "Resources");
        }
    }
}

