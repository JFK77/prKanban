using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PrKanban.Data;

namespace PrKanban.Migrations;

[DbContext(typeof(PrKanbanContext))]
[Migration("20261120143500_AddTableBulkBoardContextTranslation")]
public sealed class AddTableBulkBoardContextTranslation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            INSERT OR IGNORE INTO "AppTranslations" ("Key", "Category", "German", "English")
            VALUES ('tableBulk.projectBoardContext', 'Table', 'Projektboard: {project}', 'Project board: {project}');
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}
