using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PrKanban.Data;

namespace PrKanban.Migrations;

[DbContext(typeof(PrKanbanContext))]
[Migration("20261124221500_UpdateDeliverableActualTranslation")]
public sealed class UpdateDeliverableActualTranslation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE "AppTranslations"
            SET "German" = 'Ist (aus Tasks)', "English" = 'Actual (from tasks)'
            WHERE "Key" = 'hierarchy.actual';
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE "AppTranslations"
            SET "German" = 'Ist (h), manuell', "English" = 'Actual (h), manual'
            WHERE "Key" = 'hierarchy.actual';
            """);
    }
}
