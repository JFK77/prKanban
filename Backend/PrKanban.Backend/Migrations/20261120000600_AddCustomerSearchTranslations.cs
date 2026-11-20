using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PrKanban.Data;

namespace PrKanban.Migrations;

[DbContext(typeof(PrKanbanContext))]
[Migration("20261120000600_AddCustomerSearchTranslations")]
public sealed class AddCustomerSearchTranslations : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            INSERT OR IGNORE INTO "AppTranslations" ("Key", "Category", "German", "English") VALUES
                ('projectBoard.selectCustomer', 'ProjectBoard', 'Kunde auswählen', 'Select customer'),
                ('projectBoard.searchCustomers', 'ProjectBoard', 'Kunden suchen', 'Search customers'),
                ('projectBoard.noCustomers', 'ProjectBoard', 'Keine Kunden gefunden.', 'No customers found.');
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}
