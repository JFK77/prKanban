using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PrKanban.Data;

namespace PrKanban.Migrations;

[DbContext(typeof(PrKanbanContext))]
[Migration("20261120000200_AddAdminManagementTranslations")]
public sealed class AddAdminManagementTranslations : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        static string SqlText(string value) => "'" + value.Replace("'", "''") + "'";
        var entries = new (string Key, string German, string English)[]
        {
            ("admin.taskTypesDescription", "Task-Typen, Farben und Workflow-Pfade für das Board verwalten.", "Manage task types, colors, and workflow paths for the board."),
            ("admin.noTaskTypes", "Keine Task-Typen konfiguriert.", "No task types are configured."),
            ("admin.invalidTaskTypes", "Die gespeicherte Task-Typ-Konfiguration ist ungültig.", "The saved task type configuration is invalid."),
            ("admin.invalidTasksResponse", "Die Task-Liste konnte nicht gelesen werden.", "The task list could not be read."),
            ("admin.taskTypeName", "Task-Typ-Name", "Task type name"),
            ("admin.partialTaskTypeRename", "Einige Tasks konnten nicht umbenannt werden. Beide Typen bleiben erhalten.", "Some tasks could not be renamed. Both types have been retained."),
            ("admin.taskTypeSaved", "Task-Typ gespeichert.", "Task type saved."),
            ("admin.confirmDeleteTaskType", "Task-Typ \"{type}\" und {count} zugehörige Tasks löschen?", "Delete task type \"{type}\" and its {count} associated tasks?"),
            ("admin.partialTaskTypeDelete", "Einige Tasks konnten nicht gelöscht werden. Der Task-Typ bleibt erhalten.", "Some tasks could not be deleted. The task type has been retained."),
            ("admin.taskTypeDeleted", "Task-Typ und zugehörige Tasks gelöscht.", "Task type and associated tasks deleted."),
            ("admin.tagsDescription", "Tags, Farben und die zugehörigen Task-Zuweisungen verwalten.", "Manage tags, colors, and their task assignments."),
            ("admin.noTags", "Keine Tags konfiguriert.", "No tags are configured."),
            ("admin.invalidTags", "Die gespeicherte Tag-Konfiguration ist ungültig.", "The saved tag configuration is invalid."),
            ("admin.tagName", "Tag-Name", "Tag name"),
            ("admin.partialTagRename", "Einige Tasks konnten nicht aktualisiert werden. Beide Tags bleiben erhalten.", "Some tasks could not be updated. Both tags have been retained."),
            ("admin.tagSaved", "Tag gespeichert.", "Tag saved."),
            ("admin.partialTagDelete", "Einige Tasks konnten nicht aktualisiert werden. Der Tag bleibt erhalten.", "Some tasks could not be updated. The tag has been retained."),
            ("admin.tagDeleted", "Tag und Task-Zuweisungen gelöscht.", "Tag and task assignments deleted.")
        };

        foreach (var (key, german, english) in entries)
        {
            migrationBuilder.Sql(
                $"INSERT OR IGNORE INTO \"AppTranslations\" (\"Key\", \"Category\", \"German\", \"English\") VALUES ({SqlText(key)}, 'Administration', {SqlText(german)}, {SqlText(english)});");
        }
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Translations may have been edited after seeding; preserve them on rollback.
    }
}
