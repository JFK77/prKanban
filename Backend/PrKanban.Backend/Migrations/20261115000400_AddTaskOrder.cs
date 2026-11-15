using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PrKanban.Data;

namespace PrKanban.Migrations;

[DbContext(typeof(PrKanbanContext))]
[Migration("20261115000400_AddTaskOrder")]
public sealed class AddTaskOrder : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TABLE IF NOT EXISTS "TaskOrder" (
                "TaskId" TEXT NOT NULL CONSTRAINT "PK_TaskOrder" PRIMARY KEY,
                "Value" REAL NOT NULL,
                CONSTRAINT "FK_TaskOrder_Tasks_TaskId" FOREIGN KEY ("TaskId")
                    REFERENCES "Tasks" ("Id") ON DELETE CASCADE
            );
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "TaskOrder");
    }
}
