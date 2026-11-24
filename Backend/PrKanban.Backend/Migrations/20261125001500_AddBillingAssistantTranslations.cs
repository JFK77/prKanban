using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PrKanban.Data;

namespace PrKanban.Migrations;

[DbContext(typeof(PrKanbanContext))]
[Migration("20261125001500_AddBillingAssistantTranslations")]
public sealed class AddBillingAssistantTranslations : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            INSERT OR IGNORE INTO "AppTranslations" ("Key", "Category", "German", "English") VALUES
                ('common.remove', 'Allgemein', 'Entfernen', 'Remove'),
                ('hierarchy.deliverables', 'Administration', 'Lieferobjekte', 'Deliverables'),
                ('kpi.billingReport', 'Dashboard', 'Abrechnungsreport', 'Billing report'),
                ('kpi.reportFrom', 'Dashboard', 'Von', 'From'),
                ('kpi.reportTo', 'Dashboard', 'Bis', 'To'),
                ('kpi.reportUser', 'Dashboard', 'Benutzer', 'User'),
                ('kpi.reportPeriod', 'Dashboard', 'Zeitraum', 'Period'),
                ('kpi.reportDate', 'Dashboard', 'Datum', 'Report date'),
                ('kpi.reportCreator', 'Dashboard', 'Ersteller des Reports', 'Report creator'),
                ('kpi.companyLogo', 'Dashboard', 'Firmenlogo (PDF, oben rechts)', 'Company logo (PDF, top right)'),
                ('kpi.allDeliverables', 'Dashboard', 'Alle Lieferobjekte', 'All deliverables'),
                ('kpi.allUsers', 'Dashboard', 'Alle Benutzer', 'All users'),
                ('kpi.includeNonBillable', 'Dashboard', 'Non-Billable einbeziehen', 'Include non-billable'),
                ('kpi.showComments', 'Dashboard', 'Kommentare anzeigen', 'Show comments'),
                ('kpi.showUserNames', 'Dashboard', 'Benutzernamen anzeigen', 'Show user names'),
                ('kpi.lockedColumn', 'Dashboard', 'Gesperrt', 'Locked'),
                ('kpi.invalidRange', 'Dashboard', 'Ungültiger Zeitraum: "Von" liegt nach "Bis".', 'Invalid period: "From" is after "To".'),
                ('kpi.noEntriesInScope', 'Dashboard', 'Keine Zeiteinträge im gewählten Umfang.', 'No time entries in the selected scope.'),
                ('kpi.previewCount', 'Dashboard', '{count} Zeiteinträge im gewählten Umfang.', '{count} time entries in the selected scope.'),
                ('kpi.lockPeriod', 'Dashboard', 'Zeiten in der Periode sperren', 'Lock times in the period'),
                ('kpi.lockConfirm', 'Dashboard', '{count} Zeiteinträge im Zeitraum {period} sperren? Gesperrte Einträge können nicht mehr bearbeitet oder gelöscht werden.', 'Lock {count} time entries in the period {period}? Locked entries can no longer be edited or deleted.'),
                ('kpi.lockSuccess', 'Dashboard', '{count} Zeiteinträge gesperrt.', '{count} time entries locked.'),
                ('kpi.lockFailed', 'Dashboard', 'Sperren fehlgeschlagen.', 'Locking failed.'),
                ('kpi.nothingToLock', 'Dashboard', 'Alle Zeiteinträge im Umfang sind bereits gesperrt.', 'All time entries in scope are already locked.'),
                ('kpi.exportExcel', 'Dashboard', 'Export nach Excel', 'Export to Excel'),
                ('kpi.exportPdf', 'Dashboard', 'Export als PDF', 'Export as PDF'),
                ('kpi.exportFailed', 'Dashboard', 'Export fehlgeschlagen.', 'Export failed.'),
                ('kpi.executiveSummary', 'Dashboard', 'Kurzfassung', 'Executive summary'),
                ('kpi.entryDetails', 'Dashboard', 'Zeiteinträge im Detail', 'Time entries in detail'),
                ('kpi.metric', 'Dashboard', 'Kennzahl', 'Metric'),
                ('kpi.value', 'Dashboard', 'Wert', 'Value'),
                ('task.entryLocked', 'Task', 'Gesperrt', 'Locked'),
                ('task.clickToLock', 'Task', 'Klicken zum Sperren', 'Click to lock'),
                ('task.clickToUnlock', 'Task', 'Klicken zum Entsperren', 'Click to unlock'),
                ('task.lockedNoDelete', 'Task', 'Gesperrte Einträge können nicht gelöscht werden.', 'Locked entries cannot be deleted.'),
                ('task.unlockConfirm', 'Task', 'Diesen Zeiteintrag entsperren? Danach kann er wieder bearbeitet und gelöscht werden.', 'Unlock this time entry? Afterwards it can be edited and deleted again.'),
                ('task.lockToggleFailed', 'Task', 'Sperrstatus konnte nicht geändert werden.', 'The lock state could not be changed.'),
                ('task.deleteEntryConfirm', 'Task', 'Zeiteintrag wirklich löschen?', 'Really delete this time entry?'),
                ('task.deleteFailed', 'Task', 'Fehler beim Löschen', 'Delete failed');
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DELETE FROM "AppTranslations" WHERE "Key" IN (
                'common.remove', 'hierarchy.deliverables',
                'kpi.billingReport', 'kpi.reportFrom', 'kpi.reportTo', 'kpi.reportUser', 'kpi.reportPeriod',
                'kpi.reportDate', 'kpi.reportCreator', 'kpi.companyLogo', 'kpi.allDeliverables', 'kpi.allUsers',
                'kpi.includeNonBillable', 'kpi.showComments', 'kpi.showUserNames', 'kpi.lockedColumn',
                'kpi.invalidRange', 'kpi.noEntriesInScope', 'kpi.previewCount',
                'kpi.lockPeriod', 'kpi.lockConfirm', 'kpi.lockSuccess', 'kpi.lockFailed', 'kpi.nothingToLock',
                'kpi.exportExcel', 'kpi.exportPdf', 'kpi.exportFailed',
                'kpi.executiveSummary', 'kpi.entryDetails', 'kpi.metric', 'kpi.value',
                'task.entryLocked', 'task.clickToLock', 'task.clickToUnlock', 'task.lockedNoDelete',
                'task.unlockConfirm', 'task.lockToggleFailed', 'task.deleteEntryConfirm', 'task.deleteFailed');
            """);
    }
}
