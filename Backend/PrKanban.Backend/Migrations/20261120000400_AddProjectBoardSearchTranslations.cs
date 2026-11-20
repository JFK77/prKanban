using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PrKanban.Data;

namespace PrKanban.Migrations;

[DbContext(typeof(PrKanbanContext))]
[Migration("20261120000400_AddProjectBoardSearchTranslations")]
public sealed class AddProjectBoardSearchTranslations : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            INSERT OR IGNORE INTO "AppTranslations" ("Key", "Category", "German", "English") VALUES
                ('projectBoard.search', 'ProjectBoard', 'Projekte suchen', 'Search projects'),
                ('projectBoard.noResults', 'ProjectBoard', 'Keine Projekte gefunden.', 'No projects found.');
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}
