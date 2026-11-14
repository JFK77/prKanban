using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PrKanban.Data;

namespace PrKanban.Migrations;

[DbContext(typeof(PrKanbanContext))]
[Migration("20261115000300_AddLanguageHelp")]
public sealed class AddLanguageHelp : Migration
{
    internal const string German = "Texte für Deutsch und Englisch direkt in der Tabelle bearbeiten. Änderungen gelten nach dem erneuten Laden der App.";
    internal const string English = "Edit German and English texts directly in the table. Changes take effect after reloading the app.";
    internal const string EmptyBoardGerman = "Keine Aufgaben";
    internal const string EmptyBoardEnglish = "No tasks";
    internal const string LockedGerman = "Das Konto ist vorübergehend gesperrt.";
    internal const string LockedEnglish = "The account is temporarily locked.";
    internal const string InactiveGerman = "Das Konto ist deaktiviert.";
    internal const string InactiveEnglish = "The account is inactive.";

    protected override void Up(MigrationBuilder migrationBuilder) =>
        migrationBuilder.Sql("""
            UPDATE "AppTranslations"
            SET "German" = 'Texte für Deutsch und Englisch direkt in der Tabelle bearbeiten. Änderungen gelten nach dem erneuten Laden der App.',
                "English" = 'Edit German and English texts directly in the table. Changes take effect after reloading the app.'
            WHERE "Key" = 'admin.translationDescription'
              AND "German" = 'Texte für Deutsch und Englisch direkt in der Tabelle bearbeiten. Die Sprachumschaltung ist noch nicht verfügbar; Änderungen wirken sich derzeit nicht auf die App aus.'
              AND "English" = 'Edit German and English texts directly in the table. Language switching is not yet available; changes do not currently affect the app.';
            INSERT OR IGNORE INTO "AppTranslations" ("Key", "Category", "German", "English")
            VALUES ('board.noTasks', 'Board', 'Keine Aufgaben', 'No tasks');
            INSERT OR IGNORE INTO "AppTranslations" ("Key", "Category", "German", "English")
            VALUES ('auth.accountLocked', 'Anmeldung', 'Das Konto ist vorübergehend gesperrt.', 'The account is temporarily locked.');
            INSERT OR IGNORE INTO "AppTranslations" ("Key", "Category", "German", "English")
            VALUES ('auth.accountInactive', 'Anmeldung', 'Das Konto ist deaktiviert.', 'The account is inactive.');
            """);

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Preserve administrator edits on rollback.
    }
}
