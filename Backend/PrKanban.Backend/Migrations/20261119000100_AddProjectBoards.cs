using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PrKanban.Data;

namespace PrKanban.Migrations;

[DbContext(typeof(PrKanbanContext))]
[Migration("20261119000100_AddProjectBoards")]
public sealed class AddProjectBoards : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TABLE IF NOT EXISTS "ProjectBoardColumns" (
                "Id" TEXT NOT NULL PRIMARY KEY,
                "ProjectId" TEXT NOT NULL,
                "Name" TEXT NOT NULL,
                "MasterColumnName" TEXT NOT NULL,
                "SortOrder" INTEGER NOT NULL,
                CONSTRAINT "FK_ProjectBoardColumns_Projects_ProjectId"
                    FOREIGN KEY ("ProjectId") REFERENCES "Projects" ("Id") ON DELETE CASCADE
            );
            CREATE INDEX IF NOT EXISTS "IX_ProjectBoardColumns_ProjectId_SortOrder"
                ON "ProjectBoardColumns" ("ProjectId", "SortOrder");
            CREATE INDEX IF NOT EXISTS "IX_ProjectBoardColumns_ProjectId_MasterColumnName"
                ON "ProjectBoardColumns" ("ProjectId", "MasterColumnName");

            CREATE TABLE IF NOT EXISTS "ProjectBoardLeads" (
                "ProjectId" TEXT NOT NULL,
                "UserId" TEXT NOT NULL,
                PRIMARY KEY ("ProjectId", "UserId"),
                CONSTRAINT "FK_ProjectBoardLeads_Projects_ProjectId"
                    FOREIGN KEY ("ProjectId") REFERENCES "Projects" ("Id") ON DELETE CASCADE,
                CONSTRAINT "FK_ProjectBoardLeads_AspNetUsers_UserId"
                    FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS "ProjectBoardTaskPositions" (
                "TaskId" TEXT NOT NULL PRIMARY KEY,
                "ProjectBoardColumnId" TEXT NOT NULL,
                "SortOrder" REAL NOT NULL,
                CONSTRAINT "FK_ProjectBoardTaskPositions_Tasks_TaskId"
                    FOREIGN KEY ("TaskId") REFERENCES "Tasks" ("Id") ON DELETE CASCADE,
                CONSTRAINT "FK_ProjectBoardTaskPositions_ProjectBoardColumns_ProjectBoardColumnId"
                    FOREIGN KEY ("ProjectBoardColumnId") REFERENCES "ProjectBoardColumns" ("Id") ON DELETE CASCADE
            );
            CREATE INDEX IF NOT EXISTS "IX_ProjectBoardTaskPositions_ProjectBoardColumnId_SortOrder"
                ON "ProjectBoardTaskPositions" ("ProjectBoardColumnId", "SortOrder");

            INSERT OR IGNORE INTO "ProjectBoardColumns"
                ("Id", "ProjectId", "Name", "MasterColumnName", "SortOrder")
            SELECT 'default-' || project."Id" || '-Backlog', project."Id", 'Backlog', 'Backlog', 0
            FROM "Projects" project;
            INSERT OR IGNORE INTO "ProjectBoardColumns"
                ("Id", "ProjectId", "Name", "MasterColumnName", "SortOrder")
            SELECT 'default-' || project."Id" || '-ToDo', project."Id", 'To Do', 'ToDo', 1
            FROM "Projects" project;
            INSERT OR IGNORE INTO "ProjectBoardColumns"
                ("Id", "ProjectId", "Name", "MasterColumnName", "SortOrder")
            SELECT 'default-' || project."Id" || '-Analysis', project."Id", 'Analysis', 'Analysis', 2
            FROM "Projects" project;
            INSERT OR IGNORE INTO "ProjectBoardColumns"
                ("Id", "ProjectId", "Name", "MasterColumnName", "SortOrder")
            SELECT 'default-' || project."Id" || '-InProgress', project."Id", 'In Progress', 'InProgress', 3
            FROM "Projects" project;
            INSERT OR IGNORE INTO "ProjectBoardColumns"
                ("Id", "ProjectId", "Name", "MasterColumnName", "SortOrder")
            SELECT 'default-' || project."Id" || '-Done', project."Id", 'Done', 'Done', 4
            FROM "Projects" project;
            INSERT OR IGNORE INTO "AppTranslations" ("Key", "Category", "German", "English") VALUES
                ('projectBoard.select', 'ProjectBoard', 'Board auswählen', 'Select board'),
                ('projectBoard.master', 'ProjectBoard', 'Masterboard', 'Master board'),
                ('projectBoard.manage', 'ProjectBoard', 'Spalten verwalten', 'Manage columns'),
                ('projectBoard.manageTitle', 'ProjectBoard', 'Projektboard verwalten: {project}', 'Manage project board: {project}'),
                ('projectBoard.addColumn', 'ProjectBoard', 'Neue Spalte erstellen', 'Create a new column'),
                ('projectBoard.columnName', 'ProjectBoard', 'Spaltenname', 'Column name'),
                ('projectBoard.masterCategory', 'ProjectBoard', 'Master-Kategorie', 'Master category'),
                ('projectBoard.mapsTo', 'ProjectBoard', 'Zuordnung: {column}', 'Mapped to: {column}'),
                ('projectBoard.noColumns', 'ProjectBoard', 'Dieses Projekt hat noch keine Spalten.', 'This project has no columns yet.'),
                ('projectBoard.addTaskToColumn', 'ProjectBoard', 'Task zu {column} hinzufügen', 'Add task to {column}'),
                ('projectBoard.leads', 'ProjectBoard', 'Projektleiter', 'Project leads'),
                ('projectBoard.saveLeads', 'ProjectBoard', 'Projektleiter speichern', 'Save project leads'),
                ('projectBoard.loadUsersFailed', 'ProjectBoard', 'Benutzerliste konnte nicht geladen werden.', 'Could not load the user list.'),
                ('projectBoard.loadFailed', 'ProjectBoard', 'Projektboard konnte nicht geladen werden: {error}', 'Could not load project board: {error}'),
                ('projectBoard.nameRequired', 'ProjectBoard', 'Bitte einen Spaltennamen eingeben.', 'Enter a column name.'),
                ('projectBoard.confirmDelete', 'ProjectBoard', 'Spalte "{column}" löschen? Sie muss leer sein.', 'Delete column "{column}"? It must be empty.'),
                ('projectBoard.saved', 'ProjectBoard', 'Spalte wurde gespeichert.', 'Column saved.'),
                ('projectBoard.created', 'ProjectBoard', 'Spalte wurde erstellt.', 'Column created.'),
                ('projectBoard.deleted', 'ProjectBoard', 'Spalte wurde gelöscht.', 'Column deleted.'),
                ('projectBoard.leadsSaved', 'ProjectBoard', 'Projektleiter wurden gespeichert.', 'Project leads saved.'),
                ('projectBoard.moveLeft', 'ProjectBoard', 'Spalte nach links verschieben', 'Move column left'),
                ('projectBoard.moveRight', 'ProjectBoard', 'Spalte nach rechts verschieben', 'Move column right');

            WITH task_locations AS (
                SELECT task."Id" AS "TaskId",
                       deliverable."ProjectId" AS "ProjectId",
                       CASE lower(coalesce(task."ColumnName", ''))
                           WHEN 'backlog' THEN 'Backlog'
                           WHEN 'analysis' THEN 'Analysis'
                           WHEN 'inprogress' THEN 'InProgress'
                           WHEN 'in progress' THEN 'InProgress'
                           WHEN 'done' THEN 'Done'
                           ELSE 'ToDo'
                       END AS "MasterColumnName",
                       coalesce(taskOrder."Value", 1.0e99) AS "MasterOrder",
                       task."CreatedAt" AS "CreatedAt"
                FROM "Tasks" task
                INNER JOIN "TaskDeliverables" assignment ON assignment."TaskId" = task."Id"
                INNER JOIN "Deliverables" deliverable ON deliverable."Id" = assignment."DeliverableId"
                LEFT JOIN "TaskOrder" taskOrder ON taskOrder."TaskId" = task."Id"
            ),
            ranked_task_locations AS (
                SELECT "TaskId", "ProjectId", "MasterColumnName",
                       (row_number() OVER (
                           PARTITION BY "ProjectId", "MasterColumnName"
                           ORDER BY "MasterOrder", "CreatedAt", "TaskId") - 1) * 1024.0 AS "SortOrder"
                FROM task_locations
            )
            INSERT OR IGNORE INTO "ProjectBoardTaskPositions" ("TaskId", "ProjectBoardColumnId", "SortOrder")
            SELECT taskLocation."TaskId",
                   boardColumn."Id",
                   taskLocation."SortOrder"
            FROM ranked_task_locations taskLocation
            INNER JOIN "ProjectBoardColumns" boardColumn
                ON boardColumn."ProjectId" = taskLocation."ProjectId"
               AND boardColumn."MasterColumnName" = taskLocation."MasterColumnName"
            WHERE boardColumn."SortOrder" = (
                SELECT min(candidate."SortOrder")
                FROM "ProjectBoardColumns" candidate
                WHERE candidate."ProjectId" = boardColumn."ProjectId"
                  AND candidate."MasterColumnName" = boardColumn."MasterColumnName");
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP TABLE IF EXISTS "ProjectBoardTaskPositions";
            DROP TABLE IF EXISTS "ProjectBoardLeads";
            DROP TABLE IF EXISTS "ProjectBoardColumns";
            DELETE FROM "AppTranslations" WHERE "Key" LIKE 'projectBoard.%';
            """);
    }
}
