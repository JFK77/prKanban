using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PrKanban.Data;

namespace PrKanban.Migrations;

[DbContext(typeof(PrKanbanContext))]
[Migration("20261119000400_AddProjectBoardMappingLabel")]
public sealed class AddProjectBoardMappingLabel : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            INSERT OR IGNORE INTO "AppTranslations" ("Key", "Category", "German", "English")
            VALUES ('projectBoard.mappingLabel', 'ProjectBoard', 'Zuordnung zur Masterboard-Spalte:', 'Mapping to master board column:');
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DELETE FROM "AppTranslations" WHERE "Key" = 'projectBoard.mappingLabel';
            """);
    }
}
