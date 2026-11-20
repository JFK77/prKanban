using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PrKanban.Data;

namespace PrKanban.Migrations;

[DbContext(typeof(PrKanbanContext))]
[Migration("20261120140000_AddKpiTaskNavigationTranslations")]
public sealed class AddKpiTaskNavigationTranslations : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            INSERT OR IGNORE INTO "AppTranslations" ("Key", "Category", "German", "English") VALUES
                ('kpi.openTask', 'Dashboard', 'Task {task} im Kanban öffnen', 'Open task {task} in Kanban');
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}
