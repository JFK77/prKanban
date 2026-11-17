using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PrKanban.Data;

namespace PrKanban.Migrations;

[DbContext(typeof(PrKanbanContext))]
[Migration("20261117000200_AddImageResizeTranslation")]
public sealed class AddImageResizeTranslation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            INSERT OR IGNORE INTO "AppTranslations" ("Key", "Category", "German", "English")
            VALUES
                ('task.resizeImage', 'Task', 'Bildgröße ändern', 'Resize image'),
                ('task.resizeImageHint', 'Task', 'Bild anklicken und an den Griffen ziehen, um die Größe zu ändern.', 'Click an image and drag its handles to resize it.');
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""DELETE FROM "AppTranslations" WHERE "Key" IN ('task.resizeImage', 'task.resizeImageHint');""");
    }
}
