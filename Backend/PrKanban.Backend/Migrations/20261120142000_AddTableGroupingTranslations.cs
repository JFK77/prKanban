using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PrKanban.Data;

namespace PrKanban.Migrations;

[DbContext(typeof(PrKanbanContext))]
[Migration("20261120142000_AddTableGroupingTranslations")]
public sealed class AddTableGroupingTranslations : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            INSERT OR IGNORE INTO "AppTranslations" ("Key", "Category", "German", "English")
            VALUES ('table.groupBy', 'Table', 'Tabelle gruppieren nach', 'Group table by');
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}
