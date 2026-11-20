using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrKanban.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskStatusHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TaskStatusEvents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TaskId = table.Column<string>(type: "TEXT", nullable: false),
                    ColumnName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    SubColumnName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    OccurredAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    IsBaseline = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskStatusEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskStatusEvents_Tasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "Tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TaskStatusEvents_TaskId_Id",
                table: "TaskStatusEvents",
                columns: new[] { "TaskId", "Id" });

            migrationBuilder.Sql("""
                INSERT INTO "TaskStatusEvents" ("TaskId", "ColumnName", "SubColumnName", "OccurredAt", "IsBaseline")
                SELECT "Id",
                    CASE lower(trim("ColumnName"))
                        WHEN 'backlog' THEN 'Backlog'
                        WHEN 'analysis' THEN 'Analysis'
                        WHEN 'inprogress' THEN 'InProgress'
                        WHEN 'in progress' THEN 'InProgress'
                        WHEN 'done' THEN 'Done'
                        ELSE 'ToDo'
                    END,
                    "SubColumnName", strftime('%Y-%m-%d %H:%M:%f+00:00', 'now'), 1
                FROM "Tasks";
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TaskStatusEvents");

        }
    }
}
