using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PrKanban.Data;

namespace PrKanban.Migrations;

[DbContext(typeof(PrKanbanContext))]
[Migration("20261120140500_AddOverlappingKpiTranslations")]
public sealed class AddOverlappingKpiTranslations : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            INSERT OR IGNORE INTO "AppTranslations" ("Key", "Category", "German", "English") VALUES
                ('kpi.overlappingTasks', 'Dashboard', '{count} Aufgaben an diesem Punkt. Aufgabe auswählen.', '{count} tasks at this point. Choose a task.'),
                ('kpi.chooseTask', 'Dashboard', 'Aufgabe auswählen:', 'Choose a task:');
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}
