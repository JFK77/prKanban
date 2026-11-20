using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PrKanban.Data;

namespace PrKanban.Migrations;

[DbContext(typeof(PrKanbanContext))]
[Migration("20261120145000_AddTableExportTranslations")]
public sealed class AddTableExportTranslations : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            INSERT OR IGNORE INTO "AppTranslations" ("Key", "Category", "German", "English") VALUES
                ('table.exportExcel', 'Table', 'Excel exportieren', 'Export to Excel'),
                ('table.exportFailed', 'Table', 'Excel-Export fehlgeschlagen: {error}', 'Excel export failed: {error}');
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}
