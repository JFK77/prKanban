using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PrKanban.Data;

namespace PrKanban.Migrations;

[DbContext(typeof(PrKanbanContext))]
[Migration("20261120000500_AddBoardViewTranslations")]
public sealed class AddBoardViewTranslations : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            INSERT OR IGNORE INTO "AppTranslations" ("Key", "Category", "German", "English") VALUES
                ('views.list', 'Views', 'Liste', 'List'),
                ('projectBoard.allProjects', 'ProjectBoard', 'Alle Projekte', 'All projects'),
                ('projectBoard.context', 'ProjectBoard', 'Kunden- und Projektkontext', 'Customer and project context'),
                ('projectBoard.selectProject', 'ProjectBoard', 'Projekt auswählen', 'Select project');
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}
