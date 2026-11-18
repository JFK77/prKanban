using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PrKanban.Data;

namespace PrKanban.Migrations;

[DbContext(typeof(PrKanbanContext))]
[Migration("20261118000400_AddHierarchyOrdering")]
public sealed class AddHierarchyOrdering : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TABLE IF NOT EXISTS "HierarchyPositions" (
                "ItemKind" TEXT NOT NULL,
                "ItemId" TEXT NOT NULL,
                "ParentId" TEXT NOT NULL,
                "SortOrder" INTEGER NOT NULL,
                PRIMARY KEY ("ItemKind", "ItemId"));
            CREATE INDEX IF NOT EXISTS "IX_HierarchyPositions_ItemKind_ParentId_SortOrder"
                ON "HierarchyPositions" ("ItemKind", "ParentId", "SortOrder");
            INSERT OR IGNORE INTO "HierarchyPositions" ("ItemKind", "ItemId", "ParentId", "SortOrder")
                SELECT 'customers', item."Id", '', (
                    SELECT COUNT(*) FROM "Customers" sibling
                    WHERE sibling."Name" < item."Name"
                       OR (sibling."Name" = item."Name" AND sibling."Id" < item."Id"))
                FROM "Customers" item;
            INSERT OR IGNORE INTO "HierarchyPositions" ("ItemKind", "ItemId", "ParentId", "SortOrder")
                SELECT 'projects', item."Id", item."CustomerId", (
                    SELECT COUNT(*) FROM "Projects" sibling
                    WHERE sibling."CustomerId" = item."CustomerId"
                      AND (sibling."Name" < item."Name"
                       OR (sibling."Name" = item."Name" AND sibling."Id" < item."Id")))
                FROM "Projects" item;
            INSERT OR IGNORE INTO "HierarchyPositions" ("ItemKind", "ItemId", "ParentId", "SortOrder")
                SELECT 'deliverables', item."Id", item."ProjectId", (
                    SELECT COUNT(*) FROM "Deliverables" sibling
                    WHERE sibling."ProjectId" = item."ProjectId"
                      AND (sibling."Name" < item."Name"
                       OR (sibling."Name" = item."Name" AND sibling."Id" < item."Id")))
                FROM "Deliverables" item;
            INSERT OR IGNORE INTO "HierarchyPositions" ("ItemKind", "ItemId", "ParentId", "SortOrder")
                SELECT 'tasks', assignment."TaskId", assignment."DeliverableId", (
                    SELECT COUNT(*) FROM "TaskDeliverables" siblingAssignment
                    INNER JOIN "Tasks" siblingTask ON siblingTask."Id" = siblingAssignment."TaskId"
                    WHERE siblingAssignment."DeliverableId" = assignment."DeliverableId"
                      AND (siblingTask."UpdatedAt" > task."UpdatedAt"
                       OR (siblingTask."UpdatedAt" = task."UpdatedAt" AND siblingTask."Id" < task."Id")))
                FROM "TaskDeliverables" assignment
                INNER JOIN "Tasks" task ON task."Id" = assignment."TaskId";
            INSERT OR IGNORE INTO "AppTranslations" ("Key", "Category", "German", "English") VALUES
                ('hierarchy.dragItem', 'Hierarchy', 'Ziehen, um zu verschieben', 'Drag to move'),
                ('hierarchy.moved', 'Hierarchy', 'Position aktualisiert.', 'Position updated.');
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TABLE IF EXISTS \"HierarchyPositions\";");
        migrationBuilder.Sql("""
            DELETE FROM "AppTranslations" WHERE "Key" IN
                ('hierarchy.dragItem', 'hierarchy.moved');
            """);
    }
}
