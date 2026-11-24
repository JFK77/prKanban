using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PrKanban.Data;

namespace PrKanban.Migrations;

[DbContext(typeof(PrKanbanContext))]
[Migration("20261124235500_AddChartAxisTranslations")]
public sealed class AddChartAxisTranslations : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            INSERT OR IGNORE INTO "AppTranslations" ("Key", "Category", "German", "English") VALUES
                ('kpi.axisDate', 'Dashboard', 'Datum', 'Date'),
                ('kpi.axisTasks', 'Dashboard', 'Aufgaben', 'Tasks'),
                ('kpi.axisHours', 'Dashboard', 'Stunden (h)', 'Hours (h)'),
                ('kpi.axisWeek', 'Dashboard', 'Kalenderwoche', 'Calendar week'),
                ('kpi.axisCompletions', 'Dashboard', 'Abschlüsse (Reihenfolge)', 'Completions (order)'),
                ('kpi.axisCycleTime', 'Dashboard', 'Cycle Time (Tage)', 'Cycle time (days)'),
                ('kpi.axisEstimate', 'Dashboard', 'Schätzung (h)', 'Estimate (h)'),
                ('kpi.axisActual', 'Dashboard', 'Ist (h)', 'Actual (h)');
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DELETE FROM "AppTranslations" WHERE "Key" IN (
                'kpi.axisDate', 'kpi.axisTasks', 'kpi.axisHours', 'kpi.axisWeek',
                'kpi.axisCompletions', 'kpi.axisCycleTime', 'kpi.axisEstimate', 'kpi.axisActual');
            """);
    }
}
