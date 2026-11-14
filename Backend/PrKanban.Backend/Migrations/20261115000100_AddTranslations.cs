using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PrKanban.Data;

namespace PrKanban.Migrations;

[DbContext(typeof(PrKanbanContext))]
[Migration("20261115000100_AddTranslations")]
public sealed class AddTranslations : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // EnsureCreated already creates this table for new databases.
        migrationBuilder.Sql("""
            CREATE TABLE IF NOT EXISTS "AppTranslations" (
                "Key" TEXT NOT NULL CONSTRAINT "PK_AppTranslations" PRIMARY KEY,
                "Category" TEXT NOT NULL,
                "German" TEXT NOT NULL,
                "English" TEXT NOT NULL
            );
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "AppTranslations");
    }
}
