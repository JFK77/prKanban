using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PrKanban.Data;

namespace PrKanban.Migrations;

[DbContext(typeof(PrKanbanContext))]
[Migration("20261124232000_AddTimeTrackingTabLabelTranslation")]
public sealed class AddTimeTrackingTabLabelTranslation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            INSERT OR IGNORE INTO "AppTranslations" ("Key", "Category", "German", "English") VALUES
                ('task.timeTracking', 'Task', 'Zeiterfassung', 'Time tracking');
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DELETE FROM "AppTranslations" WHERE "Key" = 'task.timeTracking';
            """);
    }
}
