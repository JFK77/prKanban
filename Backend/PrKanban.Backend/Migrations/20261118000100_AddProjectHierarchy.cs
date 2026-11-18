using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PrKanban.Data;

namespace PrKanban.Migrations;

[DbContext(typeof(PrKanbanContext))]
[Migration("20261118000100_AddProjectHierarchy")]
public sealed class AddProjectHierarchy : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TABLE IF NOT EXISTS "Customers" ("Id" TEXT NOT NULL PRIMARY KEY, "Name" TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS "Projects" (
                "Id" TEXT NOT NULL PRIMARY KEY, "Name" TEXT NOT NULL, "CustomerId" TEXT NOT NULL,
                FOREIGN KEY ("CustomerId") REFERENCES "Customers" ("Id") ON DELETE RESTRICT);
            CREATE TABLE IF NOT EXISTS "Deliverables" (
                "Id" TEXT NOT NULL PRIMARY KEY, "Name" TEXT NOT NULL, "ProjectId" TEXT NOT NULL,
                "BudgetHours" REAL NOT NULL CONSTRAINT "CK_Deliverables_BudgetHours" CHECK ("BudgetHours" >= 0),
                FOREIGN KEY ("ProjectId") REFERENCES "Projects" ("Id") ON DELETE RESTRICT);
            CREATE TABLE IF NOT EXISTS "TaskDeliverables" (
                "TaskId" TEXT NOT NULL PRIMARY KEY, "DeliverableId" TEXT NOT NULL,
                FOREIGN KEY ("TaskId") REFERENCES "Tasks" ("Id") ON DELETE CASCADE,
                FOREIGN KEY ("DeliverableId") REFERENCES "Deliverables" ("Id") ON DELETE RESTRICT);
            CREATE INDEX IF NOT EXISTS "IX_Projects_CustomerId" ON "Projects" ("CustomerId");
            CREATE INDEX IF NOT EXISTS "IX_Deliverables_ProjectId" ON "Deliverables" ("ProjectId");
            CREATE INDEX IF NOT EXISTS "IX_TaskDeliverables_DeliverableId" ON "TaskDeliverables" ("DeliverableId");
            INSERT OR IGNORE INTO "AppTranslations" ("Key", "Category", "German", "English") VALUES
                ('hierarchy.title', 'Administration', 'Kunden und Projekte', 'Customers and projects'),
                ('hierarchy.description', 'Administration', 'Kunden, Projekte und Lieferobjekte mit Zeitbudgets verwalten.', 'Manage customers, projects and deliverables with time budgets.'),
                ('hierarchy.customers', 'Administration', 'Kunden', 'Customers'),
                ('hierarchy.projects', 'Administration', 'Projekte', 'Projects'),
                ('hierarchy.deliverables', 'Administration', 'Lieferobjekte', 'Deliverables'),
                ('hierarchy.customer', 'Administration', 'Kunde', 'Customer'),
                ('hierarchy.project', 'Administration', 'Projekt', 'Project'),
                ('hierarchy.deliverable', 'Administration', 'Lieferobjekt', 'Deliverable'),
                ('hierarchy.budget', 'Administration', 'Zeitbudget (Stunden)', 'Time budget (hours)'),
                ('hierarchy.addCustomer', 'Administration', '+ Kunde', '+ Customer'),
                ('hierarchy.addProject', 'Administration', '+ Projekt', '+ Project'),
                ('hierarchy.addDeliverable', 'Administration', '+ Lieferobjekt', '+ Deliverable'),
                ('hierarchy.select', 'Task', 'Bitte auswählen', 'Please select'),
                ('hierarchy.loadFailed', 'Administration', 'Struktur konnte nicht geladen werden.', 'Could not load the hierarchy.'),
                ('hierarchy.nameRequired', 'Administration', 'Bitte einen Namen eingeben.', 'Please enter a name.'),
                ('hierarchy.needCustomer', 'Administration', 'Bitte zuerst einen Kunden anlegen.', 'Please create a customer first.'),
                ('hierarchy.needProject', 'Administration', 'Bitte zuerst ein Projekt anlegen.', 'Please create a project first.'),
                ('hierarchy.setupRequired', 'Task', 'Ein Administrator muss zuerst Kunde, Projekt und Lieferobjekt anlegen.', 'An administrator must first create a customer, project and deliverable.'),
                ('hierarchy.saved', 'Administration', 'Gespeichert.', 'Saved.');
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("TaskDeliverables");
        migrationBuilder.DropTable("Deliverables");
        migrationBuilder.DropTable("Projects");
        migrationBuilder.DropTable("Customers");
    }
}
