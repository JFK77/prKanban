using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PrKanban.Data;

namespace PrKanban.Migrations;

[DbContext(typeof(PrKanbanContext))]
[Migration("20261120142500_AddTableSearchTranslations")]
public sealed class AddTableSearchTranslations : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            INSERT OR IGNORE INTO "AppTranslations" ("Key", "Category", "German", "English") VALUES
                ('table.sortBy', 'Table', 'Sortieren nach {column}', 'Sort by {column}'),
                ('table.filterPlaceholder', 'Table', 'Suchen …', 'Search …'),
                ('table.filterColumn', 'Table', 'In {column} suchen', 'Search {column}'),
                ('table.noMatches', 'Table', 'Keine Aufgaben entsprechen den Spaltenfiltern.', 'No tasks match the column filters.');
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}
