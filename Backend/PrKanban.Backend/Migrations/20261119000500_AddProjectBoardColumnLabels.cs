using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PrKanban.Data;

namespace PrKanban.Migrations;

[DbContext(typeof(PrKanbanContext))]
[Migration("20261119000500_AddProjectBoardColumnLabels")]
public sealed class AddProjectBoardColumnLabels : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            INSERT OR IGNORE INTO "AppTranslations" ("Key", "Category", "German", "English")
            VALUES
                ('projectBoard.thisBoardColumn', 'ProjectBoard', 'Spalte dieses Boards', 'This board column'),
                ('projectBoard.masterBoard', 'ProjectBoard', 'Masterboard', 'Master board');
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DELETE FROM "AppTranslations"
            WHERE "Key" IN ('projectBoard.thisBoardColumn', 'projectBoard.masterBoard');
            """);
    }
}
