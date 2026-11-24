using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PrKanban.Data;

namespace PrKanban.Migrations;

[DbContext(typeof(PrKanbanContext))]
[Migration("20261124222500_AddEstimateTabTranslations")]
public sealed class AddEstimateTabTranslations : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            INSERT OR IGNORE INTO "AppTranslations" ("Key", "Category", "German", "English") VALUES
                ('hierarchy.estimates', 'Administration', 'Schätzung', 'Estimate'),
                ('hierarchy.estimateHelp', 'Administration', 'Realistischer Aufwand für die Fertigstellung dieses Lieferobjekts', 'Realistic effort to complete this deliverable'),
                ('hierarchy.estimated', 'Administration', 'Geschätzt', 'Estimated'),
                ('hierarchy.remaining', 'Administration', 'Verbleibend', 'Remaining'),
                ('hierarchy.overview', 'Administration', 'Übersicht', 'Overview'),
                ('task.estimate', 'Task', 'Schätzung', 'Estimate'),
                ('task.estimatedHours', 'Task', 'Geschätzter Aufwand (Stunden)', 'Estimated effort (hours)'),
                ('task.estimateHint', 'Task', 'Realistischer Aufwand für die Fertigstellung dieser Task', 'Realistic effort to complete this task'),
                ('task.budgetHours', 'Task', 'Zeitbudget laut Auftrag', 'Contracted time budget'),
                ('task.budgetHint', 'Task', 'Beauftragtes Zeitbudget – Eingaben in d (1d = 8h) oder h werden automatisch in Stunden umgerechnet', 'Contracted time budget – entries in d (1d = 8h) or h are converted to hours automatically'),
                ('task.overview', 'Task', 'Übersicht', 'Overview'),
                ('task.estimated', 'Task', 'Geschätzt', 'Estimated'),
                ('task.logged', 'Task', 'Erfasst', 'Logged'),
                ('task.remaining', 'Task', 'Verbleibend', 'Remaining'),
                ('task.chartEstimate', 'Task', 'Schätzung', 'Estimate'),
                ('task.chartBudget', 'Task', 'Beauftragtes Budget', 'Contracted budget'),
                ('task.chartActual', 'Task', 'Tatsächliche Arbeitszeit', 'Actual work time');
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DELETE FROM "AppTranslations" WHERE "Key" IN (
                'hierarchy.estimates', 'hierarchy.estimateHelp', 'hierarchy.estimated',
                'hierarchy.remaining', 'hierarchy.overview',
                'task.estimate', 'task.estimatedHours', 'task.estimateHint',
                'task.budgetHours', 'task.budgetHint', 'task.overview', 'task.estimated',
                'task.logged', 'task.remaining',
                'task.chartEstimate', 'task.chartBudget', 'task.chartActual');
            """);
    }
}
