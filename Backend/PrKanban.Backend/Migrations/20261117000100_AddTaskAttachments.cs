using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PrKanban.Data;

namespace PrKanban.Migrations;

[DbContext(typeof(PrKanbanContext))]
[Migration("20261117000100_AddTaskAttachments")]
public sealed class AddTaskAttachments : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TABLE IF NOT EXISTS "TaskAttachments" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_TaskAttachments" PRIMARY KEY,
                "TaskId" TEXT NOT NULL,
                "FileName" TEXT NOT NULL,
                "SizeBytes" INTEGER NOT NULL,
                "IsInlineImage" INTEGER NOT NULL,
                "InlineContentType" TEXT NULL,
                "CreatedAt" TEXT NOT NULL,
                "CreatedByUserId" TEXT NOT NULL,
                "Content" BLOB NOT NULL,
                CONSTRAINT "FK_TaskAttachments_Tasks_TaskId" FOREIGN KEY ("TaskId")
                    REFERENCES "Tasks" ("Id") ON DELETE CASCADE
            );
            CREATE INDEX IF NOT EXISTS "IX_TaskAttachments_TaskId"
                ON "TaskAttachments" ("TaskId");
            INSERT OR IGNORE INTO "AppTranslations" ("Key", "Category", "German", "English")
            VALUES
                ('task.bulletedList', 'Task', 'Aufzählungsliste', 'Bulleted list'),
                ('task.numberedList', 'Task', 'Nummerierte Liste', 'Numbered list'),
                ('task.insertLink', 'Task', 'Link einfügen', 'Insert link'),
                ('task.attachments', 'Task', 'Anhänge', 'Attachments'),
                ('task.addAttachments', 'Task', 'Dateien anhängen', 'Add files'),
                ('task.attachmentLimit', 'Task', 'Bis zu 20 MB pro Datei. Beliebige Dateitypen; Anhänge werden heruntergeladen.', 'Up to 20 MB per file. Any file type; attachments are downloaded.'),
                ('task.attachmentUploadFailed', 'Task', 'Anhang konnte nicht hochgeladen werden: {error}', 'Could not upload attachment: {error}'),
                ('task.attachmentLoadFailed', 'Task', 'Anhänge konnten nicht geladen werden.', 'Could not load attachments.'),
                ('task.attachmentDeleteFailed', 'Task', 'Anhang konnte nicht gelöscht werden.', 'Could not delete attachment.'),
                ('task.removeAttachment', 'Task', 'Anhang entfernen', 'Remove attachment'),
                ('task.pendingUpload', 'Task', 'wird hochgeladen', 'upload pending'),
                ('task.invalidLink', 'Task', 'Bitte eine gültige HTTP-, HTTPS- oder E-Mail-Adresse eingeben.', 'Enter a valid HTTP, HTTPS, or email address.'),
                ('task.attachmentTooLarge', 'Task', 'Dateien dürfen höchstens 20 MB groß sein.', 'Files must be 20 MB or smaller.'),
                ('task.inlineImageInvalid', 'Task', 'Dieses Bildformat kann nicht direkt eingefügt werden. Bitte PNG, JPEG, GIF oder WebP verwenden.', 'This image format cannot be pasted inline. Use PNG, JPEG, GIF, or WebP.'),
                ('task.pasteImageHint', 'Task', 'Bilder können direkt in die Beschreibung eingefügt werden.', 'Images can be pasted directly into the description.');
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "TaskAttachments");
    }
}
