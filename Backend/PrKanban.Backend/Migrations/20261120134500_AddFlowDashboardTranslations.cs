using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PrKanban.Data;

namespace PrKanban.Migrations;

[DbContext(typeof(PrKanbanContext))]
[Migration("20261120134500_AddFlowDashboardTranslations")]
public sealed class AddFlowDashboardTranslations : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            INSERT OR IGNORE INTO "AppTranslations" ("Key", "Category", "German", "English") VALUES
                ('kpi.days', 'Dashboard', 'Tage', 'days'),
                ('kpi.last30Days', 'Dashboard', 'Letzte 30 Kalendertage (UTC).', 'Last 30 calendar days (UTC).'),
                ('kpi.noCompletedData', 'Dashboard', 'Keine vollständig erfassten Abschlüsse im Zeitraum.', 'No completions with the required recorded dates in this period.'),
                ('kpi.noChartData', 'Dashboard', 'Für diesen Zeitraum sind noch keine auswertbaren Daten vorhanden.', 'No usable data is available for this period yet.'),
                ('kpi.unknownAges', 'Dashboard', 'Aufgaben ohne bekannten Spalteneintritt sind nicht dargestellt.', 'Tasks without a known column entry date are not shown.'),
                ('kpi.coverage', 'Dashboard', 'Auswertung nach aktuellen Filtern. Statusverläufe bestehender Aufgaben beginnen erst mit der Aktivierung; unbekannte Zeiten werden nicht geschätzt. Wiedereröffnete Aufgaben zählen bei jedem erneuten Abschluss.', 'Based on current filters. History for existing tasks begins when tracking is enabled; unknown times are not estimated. Reopened tasks count at each subsequent completion.');
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}
