using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ResourceHub.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueUserResourceInteractions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM ResourceReports
                WHERE Id NOT IN (
                    SELECT MIN(Id)
                    FROM ResourceReports
                    GROUP BY ResourceId, UserId
                );
                """);

            migrationBuilder.Sql("""
                DELETE FROM ResourceRatings
                WHERE Id NOT IN (
                    SELECT MIN(Id)
                    FROM ResourceRatings
                    GROUP BY ResourceId, UserId
                );
                """);

            migrationBuilder.Sql("""
                DROP INDEX IF EXISTS IX_ResourceReports_ResourceId;
                """);

            migrationBuilder.Sql("""
                DROP INDEX IF EXISTS IX_ResourceRatings_ResourceId;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_ResourceReports_ResourceId_UserId",
                table: "ResourceReports",
                columns: new[] { "ResourceId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResourceRatings_ResourceId_UserId",
                table: "ResourceRatings",
                columns: new[] { "ResourceId", "UserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ResourceReports_ResourceId_UserId",
                table: "ResourceReports");

            migrationBuilder.DropIndex(
                name: "IX_ResourceRatings_ResourceId_UserId",
                table: "ResourceRatings");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceReports_ResourceId",
                table: "ResourceReports",
                column: "ResourceId");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceRatings_ResourceId",
                table: "ResourceRatings",
                column: "ResourceId");
        }
    }
}
