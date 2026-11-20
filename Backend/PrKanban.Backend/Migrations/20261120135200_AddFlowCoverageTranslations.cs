using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PrKanban.Data;

namespace PrKanban.Migrations;

[DbContext(typeof(PrKanbanContext))]
[Migration("20261120135200_AddFlowCoverageTranslations")]
public sealed class AddFlowCoverageTranslations : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            INSERT OR IGNORE INTO "AppTranslations" ("Key", "Category", "German", "English") VALUES
                ('kpi.meanPeriod', 'Dashboard', 'Durchschnitt der Abschlüsse der letzten 30 Kalendertage (UTC).', 'Average of completions in the last 30 calendar days (UTC).'),
                ('kpi.historyUnavailable', 'Dashboard', 'Statusverläufe fehlen für mindestens eine Aufgabe. Bitte Serverversion und Datenmigration prüfen.', 'Status history is missing for at least one task. Check the server version and data migration.');
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}
