using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PrKanban.Data;

namespace PrKanban.Migrations;

[DbContext(typeof(PrKanbanContext))]
[Migration("20261124223500_AddHoursPlaceholderTranslations")]
public sealed class AddHoursPlaceholderTranslations : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            INSERT OR IGNORE INTO "AppTranslations" ("Key", "Category", "German", "English") VALUES
                ('task.hoursPlaceholder', 'Task', 'z. B. 4h, 0,5d oder 1d 2h', 'e.g. 4h, 0.5d or 1d 2h'),
                ('task.budgetHoursPlaceholder', 'Task', 'z. B. 16h, 2d oder 2d 4h', 'e.g. 16h, 2d or 2d 4h');
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DELETE FROM "AppTranslations" WHERE "Key" IN ('task.hoursPlaceholder', 'task.budgetHoursPlaceholder');
            """);
    }
}
