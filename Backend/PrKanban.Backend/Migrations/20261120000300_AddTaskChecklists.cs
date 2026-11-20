using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PrKanban.Data;

namespace PrKanban.Migrations;

[DbContext(typeof(PrKanbanContext))]
[Migration("20261120000300_AddTaskChecklists")]
public sealed class AddTaskChecklists : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "ChecklistsJson",
            table: "Tasks",
            type: "TEXT",
            nullable: true);

        static string SqlText(string value) => "'" + value.Replace("'", "''") + "'";
        var entries = new (string Key, string German, string English)[]
        {
            ("task.checklists", "Checklisten", "Checklists"),
            ("task.checklistOpen", "{count} offen", "{count} open"),
            ("task.checklistTitle", "Checkliste", "Checklist"),
            ("task.checklistTitlePlaceholder", "Name der Checkliste", "Checklist name"),
            ("task.checklistItemPlaceholder", "Eintrag hinzufügen", "Add item"),
            ("task.addChecklist", "Checkliste hinzufügen", "Add checklist"),
            ("task.addChecklistItem", "Eintrag hinzufügen", "Add item"),
            ("task.removeChecklist", "Checkliste entfernen", "Remove checklist"),
            ("task.removeChecklistItem", "Eintrag entfernen", "Remove item"),
            ("task.hideCompleted", "Erledigte ausblenden", "Hide completed"),
            ("task.noChecklists", "Noch keine Checklisten.", "No checklists yet")
        };
        foreach (var (key, german, english) in entries)
        {
            migrationBuilder.Sql(
                $"INSERT OR IGNORE INTO \"AppTranslations\" (\"Key\", \"Category\", \"German\", \"English\") VALUES ({SqlText(key)}, 'Task', {SqlText(german)}, {SqlText(english)});");
        }
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "ChecklistsJson",
            table: "Tasks");
    }
}
