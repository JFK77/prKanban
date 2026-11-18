using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PrKanban.Data;

namespace PrKanban.Migrations;

[DbContext(typeof(PrKanbanContext))]
[Migration("20261118000300_AddViewTranslations")]
public sealed class AddViewTranslations : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            INSERT OR IGNORE INTO "AppTranslations" ("Key", "Category", "German", "English") VALUES
                ('views.label', 'Views', 'Ansichten', 'Views'),
                ('views.kanban', 'Views', 'Kanban', 'Kanban'),
                ('views.dashboard', 'Views', 'Dashboard', 'Dashboard'),
                ('views.dashboardLater', 'Views', 'Das Dashboard wird später ergänzt.', 'The dashboard will be added later.');
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DELETE FROM "AppTranslations" WHERE "Key" IN
                ('views.label', 'views.kanban', 'views.dashboard', 'views.dashboardLater');
            """);
    }
}
