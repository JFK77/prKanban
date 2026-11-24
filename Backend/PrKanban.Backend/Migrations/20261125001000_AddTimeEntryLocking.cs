using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PrKanban.Data;

namespace PrKanban.Migrations;

/// <summary>Adds billing-period lock metadata to time entries.</summary>
[DbContext(typeof(PrKanbanContext))]
[Migration("20261125001000_AddTimeEntryLocking")]
public sealed class AddTimeEntryLocking : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "Locked",
            table: "TimeEntries",
            type: "INTEGER",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<DateTime>(
            name: "LockedAt",
            table: "TimeEntries",
            type: "TEXT",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "LockedPeriod",
            table: "TimeEntries",
            type: "TEXT",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "Locked", table: "TimeEntries");
        migrationBuilder.DropColumn(name: "LockedAt", table: "TimeEntries");
        migrationBuilder.DropColumn(name: "LockedPeriod", table: "TimeEntries");
    }
}
