using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PrKanban.Data;

namespace PrKanban.Migrations;

[DbContext(typeof(PrKanbanContext))]
[Migration("20261120144000_AddProjectBulkStatusTranslations")]
public sealed class AddProjectBulkStatusTranslations : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            INSERT OR IGNORE INTO "AppTranslations" ("Key", "Category", "German", "English") VALUES
                ('tableBulk.projectStatusScope', 'Table', 'Nur ausgewählte Aufgaben des gewählten Projekts werden geändert.', 'Only selected tasks from the chosen project will be changed.'),
                ('tableBulk.invalidBoard', 'Table', 'Ungültige Projektboard-Antwort.', 'Invalid project board response.'),
                ('tableBulk.projectStatusesFailed', 'Table', 'Status für {project} konnten nicht geladen werden: {error}', 'Could not load statuses for {project}: {error}'),
                ('tableBulk.noProjectTasks', 'Table', '{count} ausgewählte Aufgaben haben kein Projekt und können keinen Projektstatus erhalten.', '{count} selected tasks have no project and cannot use a project status.'),
                ('tableBulk.projectStatusOption', 'Table', 'Projekt: {project} | Status: {status} ({count} ausgewählt)', 'Project: {project} | Status: {status} ({count} selected)'),
                ('tableBulk.projectScopeResult', 'Table', 'Projekt: {project}. {excluded} ausgewählte Aufgaben anderer Projekte oder ohne Projekt wurden nicht geändert.', 'Project: {project}. {excluded} selected tasks from other projects or without a project were not changed.');
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}
