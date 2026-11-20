using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PrKanban.Data;

namespace PrKanban.Migrations;

[DbContext(typeof(PrKanbanContext))]
[Migration("20261120143000_AddTableBulkTranslations")]
public sealed class AddTableBulkTranslations : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            INSERT OR IGNORE INTO "AppTranslations" ("Key", "Category", "German", "English") VALUES
                ('tableBulk.title', 'Table', 'Mehrfachbearbeitung', 'Bulk edit'),
                ('tableBulk.working', 'Table', 'Bearbeitung läuft …', 'Updating …'),
                ('tableBulk.selected', 'Table', '{count} ausgewählt', '{count} selected'),
                ('tableBulk.tags', 'Table', 'Tags', 'Tags'),
                ('tableBulk.searchField', 'Table', '{field} suchen', 'Search {field}'),
                ('tableBulk.useOwner', 'Table', 'Verwenden: {owner}', 'Use: {owner}'),
                ('tableBulk.add', 'Table', 'Hinzufügen', 'Add'),
                ('tableBulk.remove', 'Table', 'Entfernen', 'Remove'),
                ('tableBulk.noOptions', 'Table', 'Keine Treffer', 'No results'),
                ('tableBulk.clear', 'Table', 'Auswahl aufheben', 'Clear selection'),
                ('tableBulk.dismiss', 'Table', 'Ergebnis schließen', 'Dismiss result'),
                ('tableBulk.closeEditor', 'Table', 'Bitte zuerst den Task-Editor schließen, um ungespeicherte Änderungen zu schützen.', 'Close the task editor first to protect unsaved changes.'),
                ('tableBulk.contextChanged', 'Table', 'Auswahl oder Ansicht wurde geändert; nicht bearbeitet.', 'Selection or view changed; not updated.'),
                ('tableBulk.invalidStatus', 'Table', 'Status ist für diesen Task-Typ nicht zulässig.', 'Status is not allowed for this task type.'),
                ('tableBulk.invalidType', 'Table', 'Der Task-Typ ist für den aktuellen Status nicht zulässig.', 'Task type is not allowed for the current status.'),
                ('tableBulk.result', 'Table', '{changed} geändert, {unchanged} unverändert, {failed} fehlgeschlagen.', '{changed} changed, {unchanged} unchanged, {failed} failed.'),
                ('tableBulk.selectAll', 'Table', 'Alle gefilterten Aufgaben auswählen', 'Select all filtered tasks'),
                ('tableBulk.selectGroup', 'Table', 'Gruppe {group} auswählen', 'Select group {group}'),
                ('tableBulk.selectTask', 'Table', 'Task {task} auswählen', 'Select task {task}');
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}
