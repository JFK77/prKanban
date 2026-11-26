using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PrKanban.Data;

namespace PrKanban.Migrations;

[DbContext(typeof(PrKanbanContext))]
[Migration("20261127001000_AddAutomaticDeliverableEstimates")]
public sealed class AddAutomaticDeliverableEstimates : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "IsEstimateAutomatic",
            table: "DeliverableDetails",
            type: "INTEGER",
            nullable: false,
            defaultValue: false);

        migrationBuilder.Sql("""
            UPDATE "DeliverableDetails"
            SET "IsEstimateAutomatic" = 1
            WHERE "InitialEstimateHours" = 0;

            UPDATE "AppTranslations"
            SET "German" = 'Automatisch aus allen zugeordneten Tasks. Eingabe überschreibt den Wert; leeren stellt die Automatik wieder her.',
                "English" = 'Automatically calculated from all assigned tasks. Enter a value to override it; clear the field to restore automatic calculation.'
            WHERE "Key" = 'hierarchy.estimateHelp';

            INSERT OR IGNORE INTO "AppTranslations" ("Key", "Category", "German", "English")
            VALUES ('hierarchy.invalidEstimate', 'Administration', 'Bitte eine gültige Schätzung eingeben.', 'Please enter a valid estimate.');
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "IsEstimateAutomatic", table: "DeliverableDetails");
        migrationBuilder.Sql("""
            UPDATE "AppTranslations"
            SET "German" = 'Realistischer Aufwand für die Fertigstellung dieses Lieferobjekts',
                "English" = 'Realistic effort to complete this deliverable'
            WHERE "Key" = 'hierarchy.estimateHelp';
            DELETE FROM "AppTranslations" WHERE "Key" = 'hierarchy.invalidEstimate';
            """);
    }
}
