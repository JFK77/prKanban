using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PrKanban.Data;

namespace PrKanban.Migrations;

[DbContext(typeof(PrKanbanContext))]
[Migration("20261119000300_AddProjectBoardColumnSubLanes")]
public sealed class AddProjectBoardColumnSubLanes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "ShowSubLanes",
            table: "ProjectBoardColumns",
            type: "INTEGER",
            nullable: false,
            defaultValue: false);

        migrationBuilder.Sql("""
            INSERT OR IGNORE INTO "AppTranslations" ("Key", "Category", "German", "English")
            VALUES ('projectBoard.showSubLanes', 'ProjectBoard',
                    'Zwei Unterspalten anzeigen (Doing/Done)',
                    'Show two sublanes (Doing/Done)');
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DELETE FROM "AppTranslations" WHERE "Key" = 'projectBoard.showSubLanes';
            """);

        migrationBuilder.DropColumn(
            name: "ShowSubLanes",
            table: "ProjectBoardColumns");
    }
}
