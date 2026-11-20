using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PrKanban.Data;

namespace PrKanban.Migrations;

[DbContext(typeof(PrKanbanContext))]
[Migration("20261120000800_AddKanbanKpiTranslations")]
public sealed class AddKanbanKpiTranslations : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            INSERT OR IGNORE INTO "AppTranslations" ("Key", "Category", "German", "English") VALUES
                ('kpi.flowMetrics', 'Dashboard', 'Flow-Kennzahlen', 'Flow metrics'),
                ('kpi.wip', 'Dashboard', 'Work in Progress (WIP)', 'Work in Progress (WIP)'),
                ('kpi.wipDescription', 'Dashboard', 'Tasks in der Spalte In Progress, einschließlich ihrer Unterspalten.', 'Tasks in the In Progress column, including its sublanes.'),
                ('kpi.cycleTime', 'Dashboard', 'Cycle Time', 'Cycle Time'),
                ('kpi.leadTime', 'Dashboard', 'Lead Time', 'Lead Time'),
                ('kpi.throughput', 'Dashboard', 'Throughput', 'Throughput'),
                ('kpi.unavailable', 'Dashboard', 'Nicht verfügbar', 'Not available'),
                ('kpi.historyMissing', 'Dashboard', 'Nicht verfügbar: Statuswechsel werden noch nicht erfasst.', 'Not available: status changes are not recorded yet.'),
                ('kpi.signIn', 'Dashboard', 'Bitte anmelden, um die Kennzahlen zu sehen.', 'Sign in to see the metrics.'),
                ('kpi.loading', 'Dashboard', 'Aufgaben werden geladen …', 'Loading tasks …'),
                ('kpi.loadFailed', 'Dashboard', 'Kennzahlen nicht verfügbar: Aufgaben konnten nicht geladen werden.', 'Metrics unavailable: tasks could not be loaded.'),
                ('kpi.currentDistribution', 'Dashboard', 'Aktuelle Aufgabenverteilung', 'Current task distribution'),
                ('kpi.currentDistributionNote', 'Dashboard', 'Aktueller Stand nach Master-Board-Spalte; kein zeitlicher Verlauf.', 'Current totals by master board column; not a historical trend.'),
                ('kpi.flowCharts', 'Dashboard', 'Verlaufsdiagramme', 'Historical charts'),
                ('kpi.cfd', 'Dashboard', 'Cumulative Flow Diagram', 'Cumulative Flow Diagram'),
                ('kpi.controlChart', 'Dashboard', 'Control Chart', 'Control Chart'),
                ('kpi.scatterplot', 'Dashboard', 'Cycle Time Scatterplot', 'Cycle Time Scatterplot'),
                ('kpi.agingWip', 'Dashboard', 'Aging WIP Chart', 'Aging WIP Chart');
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}
