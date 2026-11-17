using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PrKanban.Data;

namespace PrKanban.Migrations;

[DbContext(typeof(PrKanbanContext))]
[Migration("20261117000300_RenameOwnerTranslations")]
public sealed class RenameOwnerTranslations : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE "AppTranslations" SET
                "German" = CASE WHEN "German" = 'Owner' THEN 'Zugewiesen an' ELSE "German" END,
                "English" = CASE WHEN "English" = 'Owner' THEN 'Assigned to' ELSE "English" END
                WHERE "Key" = 'board.owner';
            UPDATE "AppTranslations" SET
                "German" = CASE WHEN "German" = 'Owner filtern:' THEN 'Zugewiesene Person filtern:' ELSE "German" END,
                "English" = CASE WHEN "English" = 'Filter owner:' THEN 'Filter assignee:' ELSE "English" END
                WHERE "Key" = 'board.filterOwner';
            UPDATE "AppTranslations" SET
                "German" = CASE WHEN "German" = 'Alle Owner' THEN 'Alle Zugewiesenen' ELSE "German" END,
                "English" = CASE WHEN "English" = 'All owners' THEN 'All assignees' ELSE "English" END
                WHERE "Key" = 'board.allOwners';
            UPDATE "AppTranslations" SET
                "German" = CASE WHEN "German" = 'Alle Owner anzeigen' THEN 'Alle Zugewiesenen anzeigen' ELSE "German" END,
                "English" = CASE WHEN "English" = 'Show all owners' THEN 'Show all assignees' ELSE "English" END
                WHERE "Key" = 'board.showAllOwners';
            UPDATE "AppTranslations" SET
                "German" = CASE WHEN "German" = 'Owner auswählen oder neu erstellen' THEN 'Person auswählen oder neu erstellen' ELSE "German" END,
                "English" = CASE WHEN "English" = 'Select or create an owner' THEN 'Select or create an assignee' ELSE "English" END
                WHERE "Key" = 'task.ownerPlaceholder';
            INSERT OR IGNORE INTO "AppTranslations" ("Key", "Category", "German", "English") VALUES
                ('board.assignee', 'Board', 'Zugewiesene Person', 'Assignee'),
                ('board.assignees', 'Board', 'Zugewiesene Personen', 'Assignees');
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE "AppTranslations" SET
                "German" = CASE WHEN "German" = 'Zugewiesen an' THEN 'Owner' ELSE "German" END,
                "English" = CASE WHEN "English" = 'Assigned to' THEN 'Owner' ELSE "English" END
                WHERE "Key" = 'board.owner';
            UPDATE "AppTranslations" SET
                "German" = CASE WHEN "German" = 'Zugewiesene Person filtern:' THEN 'Owner filtern:' ELSE "German" END,
                "English" = CASE WHEN "English" = 'Filter assignee:' THEN 'Filter owner:' ELSE "English" END
                WHERE "Key" = 'board.filterOwner';
            UPDATE "AppTranslations" SET
                "German" = CASE WHEN "German" = 'Alle Zugewiesenen' THEN 'Alle Owner' ELSE "German" END,
                "English" = CASE WHEN "English" = 'All assignees' THEN 'All owners' ELSE "English" END
                WHERE "Key" = 'board.allOwners';
            UPDATE "AppTranslations" SET
                "German" = CASE WHEN "German" = 'Alle Zugewiesenen anzeigen' THEN 'Alle Owner anzeigen' ELSE "German" END,
                "English" = CASE WHEN "English" = 'Show all assignees' THEN 'Show all owners' ELSE "English" END
                WHERE "Key" = 'board.showAllOwners';
            UPDATE "AppTranslations" SET
                "German" = CASE WHEN "German" = 'Person auswählen oder neu erstellen' THEN 'Owner auswählen oder neu erstellen' ELSE "German" END,
                "English" = CASE WHEN "English" = 'Select or create an assignee' THEN 'Select or create an owner' ELSE "English" END
                WHERE "Key" = 'task.ownerPlaceholder';
            DELETE FROM "AppTranslations" WHERE "Key" IN ('board.assignee', 'board.assignees');
            """);
    }
}
