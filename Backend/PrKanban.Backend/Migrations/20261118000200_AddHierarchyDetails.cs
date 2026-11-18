using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PrKanban.Data;

namespace PrKanban.Migrations;

[DbContext(typeof(PrKanbanContext))]
[Migration("20261118000200_AddHierarchyDetails")]
public sealed class AddHierarchyDetails : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TABLE IF NOT EXISTS "CustomerDetails" (
                "CustomerId" TEXT NOT NULL PRIMARY KEY, "DebitorNo" TEXT, "CrmUrl" TEXT,
                FOREIGN KEY ("CustomerId") REFERENCES "Customers" ("Id") ON DELETE CASCADE);
            CREATE TABLE IF NOT EXISTS "ProjectDetails" (
                "ProjectId" TEXT NOT NULL PRIMARY KEY, "ShortKey" TEXT, "CrmLink" TEXT, "CrmUrl" TEXT,
                "InternalOrderNumber" TEXT, "CustomerOrderNumber" TEXT, "DefaultAssignee" TEXT,
                FOREIGN KEY ("ProjectId") REFERENCES "Projects" ("Id") ON DELETE CASCADE);
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_ProjectDetails_ShortKey" ON "ProjectDetails" ("ShortKey");
            CREATE TABLE IF NOT EXISTS "DeliverableDetails" (
                "DeliverableId" TEXT NOT NULL PRIMARY KEY,
                "InitialEstimateHours" REAL NOT NULL CONSTRAINT "CK_DeliverableDetails_InitialEstimate" CHECK ("InitialEstimateHours" >= 0),
                "ActualHours" REAL NOT NULL CONSTRAINT "CK_DeliverableDetails_Actual" CHECK ("ActualHours" >= 0),
                FOREIGN KEY ("DeliverableId") REFERENCES "Deliverables" ("Id") ON DELETE CASCADE);
            INSERT OR IGNORE INTO "AppTranslations" ("Key", "Category", "German", "English") VALUES
                ('hierarchy.treeTitle', 'Hierarchy', 'Kunden- und Projektstruktur', 'Customer and project structure'),
                ('hierarchy.treeDescription', 'Hierarchy', 'Kunden, Projekte und Lieferobjekte verwalten', 'Manage customers, projects and deliverables'),
                ('hierarchy.newCustomer', 'Hierarchy', 'Kunde anlegen', 'New customer'),
                ('hierarchy.newProject', 'Hierarchy', 'Projekt anlegen', 'New project'),
                ('hierarchy.newDeliverable', 'Hierarchy', 'Lieferobjekt anlegen', 'New deliverable'),
                ('hierarchy.debitorNo', 'Hierarchy', 'Debitorennummer', 'Debtor number'),
                ('hierarchy.crmUrl', 'Hierarchy', 'CRM URL', 'CRM URL'),
                ('hierarchy.crmLink', 'Hierarchy', 'CRM Link', 'CRM link'),
                ('hierarchy.shortKey', 'Hierarchy', 'Projektkürzel', 'Project short key'),
                ('hierarchy.keyHelp', 'Hierarchy', '2–12 Buchstaben/Ziffern, beginnt mit einem Buchstaben. Nach dem Speichern unveränderlich.', '2–12 letters/digits, starting with a letter. Cannot be changed after saving.'),
                ('hierarchy.internalOrder', 'Hierarchy', 'Interne Projektauftragsnummer', 'Internal project order number'),
                ('hierarchy.customerOrder', 'Hierarchy', 'Kundenauftragsnummer', 'Customer order number'),
                ('hierarchy.defaultAssignee', 'Hierarchy', 'Standard-Zuweisung', 'Default assignee'),
                ('hierarchy.estimate', 'Hierarchy', 'Initiale Schätzung (h)', 'Initial estimate (h)'),
                ('hierarchy.actual', 'Hierarchy', 'Ist (h), manuell', 'Actual (h), manual'),
                ('hierarchy.empty', 'Hierarchy', 'Noch keine Kunden vorhanden.', 'No customers yet.'),
                ('hierarchy.pick', 'Hierarchy', 'Ein Element auswählen oder neu anlegen.', 'Select an item or create a new one.'),
                ('hierarchy.readOnly', 'Hierarchy', 'Nur Administratoren können Änderungen vornehmen.', 'Only administrators can make changes.'),
                ('hierarchy.open', 'Hierarchy', 'Kunden und Projekte', 'Customers and projects'),
                ('hierarchy.companyName', 'Hierarchy', 'Firmenname', 'Company name'),
                ('hierarchy.projectName', 'Hierarchy', 'Projektname', 'Project name'),
                ('hierarchy.deliverableTitle', 'Hierarchy', 'Titel', 'Title'),
                ('hierarchy.view', 'Hierarchy', 'Anzeigen', 'View'),
                ('hierarchy.discard', 'Hierarchy', 'Ungespeicherte Änderungen verwerfen?', 'Discard unsaved changes?'),
                ('hierarchy.projectKeyRequired', 'Hierarchy', 'Bitte zuerst in Kunden und Projekte ein Projektkürzel hinterlegen.', 'Please set a project short key in Customers and projects first.'),
                ('hierarchy.signIn', 'Hierarchy', 'Bitte anmelden, um die Struktur zu öffnen.', 'Please sign in to view the hierarchy.'),
                ('hierarchy.keyExists', 'Hierarchy', 'Dieses Projektkürzel ist bereits vergeben.', 'This project short key is already in use.'),
                ('hierarchy.keyLocked', 'Hierarchy', 'Das Projektkürzel kann nicht mehr geändert werden.', 'The project short key cannot be changed.');
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("DeliverableDetails");
        migrationBuilder.DropTable("ProjectDetails");
        migrationBuilder.DropTable("CustomerDetails");
    }
}
