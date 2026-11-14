using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PrKanban.Models;
using Task = PrKanban.Models.Task;

namespace PrKanban.Data
{
    /// <summary>
    /// EF Core DbContext for prKanban database
    /// Inherits from IdentityDbContext for ASP.NET Core Identity support
    /// </summary>
    public class PrKanbanContext : IdentityDbContext<ApplicationUser>
    {
        /// <summary>
        /// DbSet for tasks
        /// </summary>
        public DbSet<Task> Tasks { get; set; }

        /// <summary>
        /// DbSet for board configurations
        /// </summary>
        public DbSet<BoardConfig> BoardConfigs { get; set; }

        public DbSet<TranslationEntry> AppTranslations { get; set; }

        /// <summary>
        /// Constructor accepting options
        /// </summary>
        public PrKanbanContext(DbContextOptions<PrKanbanContext> options)
            : base(options)
        {
        }

        /// <summary>
        /// Configure model relationships and constraints
        /// </summary>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<TranslationEntry>(entity =>
            {
                entity.HasKey(e => e.Key);
                entity.Property(e => e.Key).HasMaxLength(120);
                entity.Property(e => e.Category).IsRequired().HasMaxLength(100);
                entity.Property(e => e.German).IsRequired().HasMaxLength(2048);
                entity.Property(e => e.English).IsRequired().HasMaxLength(2048);
            });

            // Configure ApplicationUser (extends IdentityUser)
            modelBuilder.Entity<ApplicationUser>(entity =>
            {
                entity.Property(e => e.FullName).HasMaxLength(256);
                entity.Property(e => e.ProfilePictureUrl).HasMaxLength(2000);
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.UpdatedAt).IsRequired();

                // One-to-many: ApplicationUser -> Tasks
                entity.HasMany(e => e.Tasks)
                    .WithOne(t => t.User)
                    .HasForeignKey(t => t.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                // One-to-one: ApplicationUser -> BoardConfig
                entity.HasOne(e => e.BoardConfig)
                    .WithOne(bc => bc.User)
                    .HasForeignKey<BoardConfig>(bc => bc.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure Task
            modelBuilder.Entity<Task>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(255);
                entity.Property(e => e.TaskType).HasMaxLength(50);
                entity.Property(e => e.Owner).HasMaxLength(256);
                entity.Property(e => e.Customer).HasMaxLength(256);
                entity.Property(e => e.ColumnName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.SubColumnName).HasMaxLength(100);
                entity.Property(e => e.UserId).IsRequired();
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.UpdatedAt).IsRequired();

                // Indexes for query performance
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.ColumnName);
                entity.HasIndex(e => new { e.UserId, e.IsArchived });
                entity.HasIndex(e => e.CreatedAt);
                entity.HasIndex(e => e.UpdatedAt);
            });

            // Configure BoardConfig
            modelBuilder.Entity<BoardConfig>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.UserId).IsRequired();
                entity.Property(e => e.Theme).HasDefaultValue("light").HasMaxLength(50);
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.UpdatedAt).IsRequired();

                // Unique constraint: one config per user
                entity.HasIndex(e => e.UserId).IsUnique();
            });
        }

        /// <summary>
        /// Override SaveChanges to update timestamps
        /// </summary>
        public override int SaveChanges()
        {
            UpdateTimestamps();
            return base.SaveChanges();
        }

        /// <summary>
        /// Override SaveChangesAsync to update timestamps
        /// </summary>
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateTimestamps();
            return await base.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Update UpdatedAt timestamp for modified entities
        /// </summary>
        private void UpdateTimestamps()
        {
            var entries = ChangeTracker.Entries()
                .Where(e => e.Entity is ApplicationUser or Task or BoardConfig);

            foreach (var entry in entries)
            {
                if (entry.State == EntityState.Modified)
                {
                    var entity = entry.Entity;
                    if (entity is ApplicationUser user)
                        user.UpdatedAt = DateTime.UtcNow;
                    else if (entity is Task task)
                        task.UpdatedAt = DateTime.UtcNow;
                    else if (entity is BoardConfig config)
                        config.UpdatedAt = DateTime.UtcNow;
                }
            }
        }
    }
}
