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
        public DbSet<TaskStatusEvent> TaskStatusEvents { get; set; }

        public DbSet<TaskAttachment> TaskAttachments { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<Deliverable> Deliverables { get; set; }
        public DbSet<TaskDeliverable> TaskDeliverables { get; set; }
        public DbSet<HierarchyPosition> HierarchyPositions { get; set; }
        public DbSet<ProjectBoardColumn> ProjectBoardColumns { get; set; }
        public DbSet<ProjectBoardLead> ProjectBoardLeads { get; set; }
        public DbSet<ProjectBoardTaskPosition> ProjectBoardTaskPositions { get; set; }

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

            modelBuilder.Entity<Customer>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Name).IsRequired().HasMaxLength(200);
                entity.HasOne(x => x.Details).WithOne().HasForeignKey<CustomerDetails>(x => x.CustomerId).OnDelete(DeleteBehavior.Cascade);
            });
            modelBuilder.Entity<Project>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Name).IsRequired().HasMaxLength(200);
                entity.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(x => x.Details).WithOne().HasForeignKey<ProjectDetails>(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
            });
            modelBuilder.Entity<Deliverable>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Name).IsRequired().HasMaxLength(200);
                entity.ToTable(t => t.HasCheckConstraint("CK_Deliverables_BudgetHours", "\"BudgetHours\" >= 0"));
                entity.HasOne(x => x.Project).WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(x => x.Details).WithOne().HasForeignKey<DeliverableDetails>(x => x.DeliverableId).OnDelete(DeleteBehavior.Cascade);
            });
            modelBuilder.Entity<CustomerDetails>().HasKey(x => x.CustomerId);
            modelBuilder.Entity<ProjectDetails>(entity =>
            {
                entity.HasKey(x => x.ProjectId);
                entity.HasIndex(x => x.ShortKey).IsUnique();
            });
            modelBuilder.Entity<DeliverableDetails>(entity =>
            {
                entity.HasKey(x => x.DeliverableId);
                entity.ToTable(t =>
                {
                    t.HasCheckConstraint("CK_DeliverableDetails_InitialEstimate", "\"InitialEstimateHours\" >= 0");
                    t.HasCheckConstraint("CK_DeliverableDetails_Actual", "\"ActualHours\" >= 0");
                });
            });
            modelBuilder.Entity<TaskDeliverable>(entity =>
            {
                entity.HasKey(x => x.TaskId);
                entity.HasOne<Task>().WithOne(x => x.DeliverableAssignment)
                    .HasForeignKey<TaskDeliverable>(x => x.TaskId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(x => x.Deliverable).WithMany().HasForeignKey(x => x.DeliverableId).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<HierarchyPosition>(entity =>
            {
                entity.HasKey(x => new { x.ItemKind, x.ItemId });
                entity.Property(x => x.ItemKind).IsRequired().HasMaxLength(20);
                entity.Property(x => x.ItemId).IsRequired().HasMaxLength(256);
                entity.Property(x => x.ParentId).IsRequired().HasMaxLength(256);
                entity.HasIndex(x => new { x.ItemKind, x.ParentId, x.SortOrder });
            });
            modelBuilder.Entity<ProjectBoardColumn>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id).IsRequired().HasMaxLength(256);
                entity.Property(x => x.ProjectId).IsRequired().HasMaxLength(256);
                entity.Property(x => x.Name).IsRequired().HasMaxLength(100);
                entity.Property(x => x.MasterColumnName).IsRequired().HasMaxLength(100);
                entity.Property(x => x.ShowSubLanes).IsRequired().HasDefaultValue(false);
                entity.HasOne<Project>().WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(x => new { x.ProjectId, x.SortOrder });
                entity.HasIndex(x => new { x.ProjectId, x.MasterColumnName });
            });
            modelBuilder.Entity<ProjectBoardLead>(entity =>
            {
                entity.HasKey(x => new { x.ProjectId, x.UserId });
                entity.Property(x => x.ProjectId).IsRequired().HasMaxLength(256);
                entity.Property(x => x.UserId).IsRequired();
                entity.HasOne<Project>().WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            });
            modelBuilder.Entity<ProjectBoardTaskPosition>(entity =>
            {
                entity.HasKey(x => x.TaskId);
                entity.Property(x => x.TaskId).IsRequired();
                entity.Property(x => x.ProjectBoardColumnId).IsRequired().HasMaxLength(256);
                entity.HasOne<Task>().WithOne(x => x.ProjectBoardPosition)
                    .HasForeignKey<ProjectBoardTaskPosition>(x => x.TaskId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(x => x.Column).WithMany().HasForeignKey(x => x.ProjectBoardColumnId).OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(x => new { x.ProjectBoardColumnId, x.SortOrder });
            });

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
                entity.HasOne(e => e.Ordering)
                    .WithOne()
                    .HasForeignKey<TaskOrder>(e => e.TaskId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(255);
                entity.Property(e => e.TaskType).HasMaxLength(50);
                entity.Property(e => e.Owner).HasMaxLength(256);
                entity.Property(e => e.Customer).HasMaxLength(256);
                entity.Property(e => e.ColumnName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.SubColumnName).HasMaxLength(100);
                entity.Property(e => e.UserId).IsRequired();
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.UpdatedAt).IsRequired();
                entity.HasIndex(e => e.TaskNumber).IsUnique();

                // Indexes for query performance
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.ColumnName);
                entity.HasIndex(e => new { e.UserId, e.IsArchived });
                entity.HasIndex(e => e.CreatedAt);
                entity.HasIndex(e => e.UpdatedAt);
            });

            modelBuilder.Entity<TaskOrder>().HasKey(e => e.TaskId);

            modelBuilder.Entity<TaskStatusEvent>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.TaskId).IsRequired();
                entity.Property(e => e.ColumnName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.SubColumnName).HasMaxLength(100);
                entity.HasIndex(e => new { e.TaskId, e.Id });
                entity.HasOne<Task>().WithMany(t => t.StatusHistory)
                    .HasForeignKey(e => e.TaskId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<TaskAttachment>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasMaxLength(32);
                entity.Property(e => e.TaskId).IsRequired();
                entity.Property(e => e.FileName).IsRequired().HasMaxLength(255);
                entity.Property(e => e.SizeBytes).IsRequired();
                entity.Property(e => e.IsInlineImage).IsRequired();
                entity.Property(e => e.InlineContentType).HasMaxLength(32);
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.CreatedByUserId).IsRequired();
                entity.Property(e => e.Content).IsRequired();
                entity.HasIndex(e => e.TaskId);
                entity.HasOne<Task>()
                    .WithMany()
                    .HasForeignKey(e => e.TaskId)
                    .OnDelete(DeleteBehavior.Cascade);
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
            RecordTaskStatuses();
            return base.SaveChanges();
        }

        /// <summary>
        /// Override SaveChangesAsync to update timestamps
        /// </summary>
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateTimestamps();
            RecordTaskStatuses();
            return await base.SaveChangesAsync(cancellationToken);
        }

        private void RecordTaskStatuses()
        {
            var now = DateTimeOffset.UtcNow;
            foreach (var entry in ChangeTracker.Entries<Task>().ToArray())
            {
                if (entry.State != EntityState.Added && entry.State != EntityState.Modified)
                    continue;

                var column = MasterBoardColumns.Normalize(entry.Entity.ColumnName);
                var subColumn = entry.Entity.SubColumnName;
                if (entry.State == EntityState.Modified &&
                    MasterBoardColumns.Normalize(entry.Property(t => t.ColumnName).OriginalValue) == column &&
                    entry.Property(t => t.SubColumnName).OriginalValue == subColumn)
                    continue;

                TaskStatusEvents.Add(new TaskStatusEvent
                {
                    TaskId = entry.Entity.Id,
                    ColumnName = column,
                    SubColumnName = subColumn,
                    OccurredAt = entry.State == EntityState.Added
                        ? new DateTimeOffset(DateTime.SpecifyKind(entry.Entity.CreatedAt, DateTimeKind.Utc))
                        : now
                });
            }
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
