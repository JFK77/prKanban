using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PrKanban.Data;

namespace PrKanban.Migrations;

[DbContext(typeof(PrKanbanContext))]
[Migration("20261124231000_AddTimeTrackingTabTranslations")]
public sealed class AddTimeTrackingTabTranslations : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            INSERT OR IGNORE INTO "AppTranslations" ("Key", "Category", "German", "English") VALUES
                ('common.add', 'Allgemein', 'Hinzufügen', 'Add'),
                ('task.addTimeEntry', 'Task', 'Zeit erfassen', 'Log time'),
                ('task.date', 'Task', 'Datum', 'Date'),
                ('task.comment', 'Task', 'Kommentar (optional)', 'Comment (optional)'),
                ('task.commentPlaceholder', 'Task', 'z.B. Anforderungen analysiert', 'e.g. analyzed requirements'),
                ('task.billable', 'Task', 'Abrechenbar', 'Billable'),
                ('task.noTimeEntries', 'Task', 'Noch keine Zeiteinträge', 'No time entries yet'),
                ('task.timeEntries', 'Task', 'Zeiteinträge', 'Time entries'),
                ('task.totalHours', 'Task', 'Summe', 'Total'),
                ('task.billableHours', 'Task', 'Abrechenbar', 'Billable'),
                ('task.timeFrom', 'Task', 'Von', 'From'),
                ('task.timeTo', 'Task', 'Bis', 'To'),
                ('task.timeDuration', 'Task', 'Dauer', 'Duration'),
                ('task.timeNow', 'Task', 'jetzt', 'now'),
                ('task.timeAddFrom', 'Task', 'Ab Von:', 'Add to start:'),
                ('task.timeSubtract', 'Task', 'Abzug:', 'Subtract:'),
                ('task.timeUser', 'Task', 'Benutzer', 'User'),
                ('task.timeComment', 'Task', 'Kommentar', 'Comment'),
                ('task.timeAction', 'Task', 'Aktion', 'Action'),
                ('task.weekLogTime', 'Task', 'Wochenzeit erfassen', 'Log weekly time'),
                ('task.weekLabel', 'Task', 'KW {week}', 'Week {week}'),
                ('task.weekPrev', 'Task', '← Vorwoche', '← Previous week'),
                ('task.weekNext', 'Task', 'Folgewoche →', 'Next week →'),
                ('task.weekToday', 'Task', 'Heute', 'Today'),
                ('task.weekSum', 'Task', 'Wochensumme (Mo–Fr):', 'Week total (Mon–Fri):'),
                ('task.weekFill', 'Task', 'Woche auffüllen', 'Fill week'),
                ('task.weekClear', 'Task', 'Leeren', 'Clear'),
                ('task.weekHoursPlaceholder', 'Task', 'z.B. 8h', 'e.g. 8h'),
                ('task.weekMinutes', 'Task', 'Min.', 'min'),
                ('task.dayMon', 'Task', 'Mo', 'Mon'),
                ('task.dayTue', 'Task', 'Di', 'Tue'),
                ('task.dayWed', 'Task', 'Mi', 'Wed'),
                ('task.dayThu', 'Task', 'Do', 'Thu'),
                ('task.dayFri', 'Task', 'Fr', 'Fri');
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DELETE FROM "AppTranslations" WHERE "Key" IN (
                'common.add', 'task.addTimeEntry', 'task.date', 'task.comment', 'task.commentPlaceholder',
                'task.billable', 'task.noTimeEntries', 'task.timeEntries', 'task.totalHours', 'task.billableHours',
                'task.timeFrom', 'task.timeTo', 'task.timeDuration', 'task.timeNow', 'task.timeAddFrom',
                'task.timeSubtract', 'task.timeUser', 'task.timeComment', 'task.timeAction',
                'task.weekLogTime', 'task.weekLabel', 'task.weekPrev', 'task.weekNext', 'task.weekToday',
                'task.weekSum', 'task.weekFill', 'task.weekClear', 'task.weekHoursPlaceholder', 'task.weekMinutes',
                'task.dayMon', 'task.dayTue', 'task.dayWed', 'task.dayThu', 'task.dayFri');
            """);
    }
}
