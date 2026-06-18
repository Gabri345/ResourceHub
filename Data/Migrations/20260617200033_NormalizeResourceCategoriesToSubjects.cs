using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ResourceHub.Data.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeResourceCategoriesToSubjects : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE Resources
                SET Category = 'Informatics'
                WHERE Category = 'Informatics';
                """);

            migrationBuilder.Sql("""
                UPDATE Resources
                SET Category = 'Other'
                WHERE Category IN ('Plans', 'Images', 'Presentations', 'Homework', 'Projects')
                   OR Category IS NULL
                   OR TRIM(Category) = '';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
