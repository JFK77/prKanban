using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrKanban.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskBudgetHours : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "BudgetHours",
                table: "Tasks",
                type: "REAL",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BudgetHours",
                table: "Tasks");
        }
    }
}
