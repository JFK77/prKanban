using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PrKanban.Data;

namespace PrKanban.Migrations;

[DbContext(typeof(PrKanbanContext))]
[Migration("20261120000100_AddProjectTaskNumbers")]
public sealed class AddProjectTaskNumbers : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "LastTaskNumber",
            table: "ProjectDetails",
            type: "INTEGER",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<string>(
            name: "TaskNumber",
            table: "Tasks",
            type: "TEXT",
            nullable: true);

        migrationBuilder.Sql("""
            WITH numbered_tasks AS (
                SELECT task."Id" AS "TaskId",
                       projectDetails."ShortKey" || '-' ||
                           row_number() OVER (
                               PARTITION BY deliverable."ProjectId"
                               ORDER BY task."CreatedAt", task."Id") AS "TaskNumber"
                FROM "Tasks" task
                INNER JOIN "TaskDeliverables" assignment ON assignment."TaskId" = task."Id"
                INNER JOIN "Deliverables" deliverable ON deliverable."Id" = assignment."DeliverableId"
                INNER JOIN "ProjectDetails" projectDetails ON projectDetails."ProjectId" = deliverable."ProjectId"
                WHERE projectDetails."ShortKey" IS NOT NULL
            )
            UPDATE "Tasks"
            SET "TaskNumber" = (
                SELECT numbered_tasks."TaskNumber"
                FROM numbered_tasks
                WHERE numbered_tasks."TaskId" = "Tasks"."Id")
            WHERE "Id" IN (SELECT "TaskId" FROM numbered_tasks);

            UPDATE "ProjectDetails"
            SET "LastTaskNumber" = (
                SELECT count(*)
                FROM "Tasks" task
                INNER JOIN "TaskDeliverables" assignment ON assignment."TaskId" = task."Id"
                INNER JOIN "Deliverables" deliverable ON deliverable."Id" = assignment."DeliverableId"
                WHERE deliverable."ProjectId" = "ProjectDetails"."ProjectId"
                  AND task."TaskNumber" IS NOT NULL);
            """);

        migrationBuilder.CreateIndex(
            name: "IX_Tasks_TaskNumber",
            table: "Tasks",
            column: "TaskNumber",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Tasks_TaskNumber",
            table: "Tasks");

        migrationBuilder.DropColumn(
            name: "LastTaskNumber",
            table: "ProjectDetails");

        migrationBuilder.DropColumn(
            name: "TaskNumber",
            table: "Tasks");
    }
}
