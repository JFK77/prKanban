using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PrKanban.Data;

namespace PrKanban.Migrations;

[DbContext(typeof(PrKanbanContext))]
[Migration("20261120141500_AddTaskTableTranslations")]
public sealed class AddTaskTableTranslations : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            INSERT OR IGNORE INTO "AppTranslations" ("Key", "Category", "German", "English") VALUES
                ('views.table', 'Views', 'Tabelle', 'Table'),
                ('table.taskId', 'Table', 'Task-ID', 'Task ID'),
                ('table.status', 'Table', 'Status', 'Status'),
                ('table.unassigned', 'Table', 'Nicht zugeordnet', 'Unassigned'),
                ('table.signIn', 'Table', 'Bitte anmelden, um Aufgaben zu sehen.', 'Sign in to see tasks.'),
                ('table.offline', 'Table', 'Aufgaben konnten nicht geladen werden. Die lokale Sicherung wird angezeigt.', 'Tasks could not be loaded. Showing the local backup.');
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}
