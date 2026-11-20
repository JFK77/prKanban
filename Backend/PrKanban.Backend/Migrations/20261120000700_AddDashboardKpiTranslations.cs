using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PrKanban.Data;

namespace PrKanban.Migrations;

[DbContext(typeof(PrKanbanContext))]
[Migration("20261120000700_AddDashboardKpiTranslations")]
public sealed class AddDashboardKpiTranslations : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            INSERT OR IGNORE INTO "AppTranslations" ("Key", "Category", "German", "English") VALUES
                ('views.dashboardKpis', 'Views', 'Dashboard-Kennzahlen', 'Dashboard KPIs'),
                ('views.kanbanKpis', 'Views', 'Kanban KPIs', 'Kanban KPIs'),
                ('views.budgetKpis', 'Views', 'Budget KPIs', 'Budget KPIs');
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}
