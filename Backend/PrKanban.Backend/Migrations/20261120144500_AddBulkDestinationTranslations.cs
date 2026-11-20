using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PrKanban.Data;

namespace PrKanban.Migrations;

[DbContext(typeof(PrKanbanContext))]
[Migration("20261120144500_AddBulkDestinationTranslations")]
public sealed class AddBulkDestinationTranslations : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            INSERT OR IGNORE INTO "AppTranslations" ("Key", "Category", "German", "English") VALUES
                ('tableBulk.titleScope', 'Table', 'Alle ausgewählten Aufgaben erhalten denselben Titel.', 'All selected tasks will receive the same title.'),
                ('tableBulk.newTitle', 'Table', 'Neuer Titel', 'New title'),
                ('tableBulk.applyTitle', 'Table', 'Titel übernehmen', 'Apply title'),
                ('tableBulk.destinationScope', 'Table', 'Ziel-Lieferobjekt auswählen. Alle ausgewählten Aufgaben wechseln in dessen Projekt.', 'Choose a destination deliverable. All selected tasks will move to its project.'),
                ('tableBulk.destinationOption', 'Table', 'Projekt: {project} | Lieferobjekt: {deliverable}', 'Project: {project} | Deliverable: {deliverable}'),
                ('tableBulk.backToProjects', 'Table', '← Anderes Projekt wählen', '← Choose another project'),
                ('tableBulk.noDeliverables', 'Table', 'Dieses Projekt hat keine Lieferobjekte.', 'This project has no deliverables.'),
                ('tableBulk.destinationsFailed', 'Table', 'Ziele konnten nicht geladen werden: {error}', 'Could not load destinations: {error}'),
                ('tableBulk.destinationChanged', 'Table', 'Das Ziel wurde geändert oder existiert nicht mehr. Bitte erneut auswählen.', 'The destination changed or no longer exists. Choose it again.');
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}
