using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PrKanban.Data
{
    /// <summary>
    /// Factory for creating PrKanbanContext instances during design-time operations (e.g., migrations)
    /// </summary>
    public class PrKanbanContextFactory : IDesignTimeDbContextFactory<PrKanbanContext>
    {
        public PrKanbanContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<PrKanbanContext>();
            var connectionString = "Data Source=C:\\prKanban-migration\\Data\\prkanban.db";
            optionsBuilder.UseSqlite(connectionString);

            return new PrKanbanContext(optionsBuilder.Options);
        }
    }
}
