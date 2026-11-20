using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PrKanban.Data;

namespace PrKanban.Migrations;

[DbContext(typeof(PrKanbanContext))]
[Migration("20261120141000_AddEmptyChartTranslation")]
public sealed class AddEmptyChartTranslation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            INSERT OR IGNORE INTO "AppTranslations" ("Key", "Category", "German", "English") VALUES
                ('kpi.noDataForPeriod', 'Dashboard', 'Für diesen Zeitraum sind noch keine Daten verfügbar. Bitte überprüfen Sie auch die Filter.', 'No data is available for this period yet. Please check filters as well.');
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""DELETE FROM "AppTranslations" WHERE "Key" = 'kpi.noDataForPeriod';""");
    }
}
