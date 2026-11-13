# Phase 2: Database Design (EF Core + SQLite)

**Date**: 2026-11-14  
**Status**: IN PROGRESS  
**Target Implementation**: Phase 3  

---

## EXECUTIVE SUMMARY

This document details the complete SQLite database design for prKanban, including:
- ✅ EF Core DbContext models
- ✅ Database schema and relationships
- ✅ Data type specifications
- ✅ Indexes and constraints
- ✅ Migration strategy
- ✅ Connection pooling configuration

---

## 1. DATABASE FUNDAMENTALS

### SQLite Configuration

**File Location**: `D:\Apps\prKanban\Data\prkanban.db`

**Connection String**:
```
Data Source=D:\Apps\prKanban\Data\prkanban.db;Mode=ReadWrite;Cache=Shared;Journal Mode=Wal;Busy Timeout=5000;
```

**Configuration Details**:
| Setting | Value | Reason |
|---|---|---|
| Mode | ReadWrite | Both read and write operations |
| Cache | Shared | Allows connection sharing |
| Journal Mode | WAL | Write-Ahead Logging (better concurrency) |
| Busy Timeout | 5000ms | Wait 5 seconds if database locked |
| Synchronous | NORMAL | Balance between safety and speed |
| Foreign Keys | ON | Enforce referential integrity |

---

## 2. ENTITY MODELS (EF CORE)

### User Entity

```csharp
namespace PrKanban.Models
{
    using System;
    using System.Collections.Generic;
    
    /// <summary>
    /// Represents a user authenticated via OAuth2/OIDC
    /// </summary>
    public class User
    {
        /// <summary>
        /// Unique identifier (UUID format: "user_<guid>")
        /// </summary>
        public string Id { get; set; } = $"user_{Guid.NewGuid()}";
        
        /// <summary>
        /// Email address (unique across system)
        /// </summary>
        public string Email { get; set; }
        
        /// <summary>
        /// Display name from OAuth provider
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// OAuth provider name: "Google", "Microsoft", "AzureAD"
        /// </summary>
        public string Provider { get; set; }
        
        /// <summary>
        /// Provider-specific user ID (e.g., Google subject claim)
        /// </summary>
        public string ProviderId { get; set; }
        
        /// <summary>
        /// Profile picture URL from provider
        /// </summary>
        public string ProfilePicture { get; set; }
        
        /// <summary>
        /// UTC timestamp when user was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// UTC timestamp when user was last updated
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Last login timestamp (nullable)
        /// </summary>
        public DateTime? LastLoginAt { get; set; }
        
        // Navigation properties
        /// <summary>
        /// Collection of tasks owned by this user
        /// </summary>
        public virtual ICollection<Task> Tasks { get; set; } = new List<Task>();
        
        /// <summary>
        /// Board configuration for this user (1:1 relationship)
        /// </summary>
        public virtual BoardConfig BoardConfig { get; set; }
    }
}
```

### Task Entity

```csharp
namespace PrKanban.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json;
    
    /// <summary>
    /// Represents a single task/card on the Kanban board
    /// </summary>
    public class Task
    {
        /// <summary>
        /// Unique identifier ("task_<guid>")
        /// </summary>
        public string Id { get; set; } = $"task_{Guid.NewGuid()}";
        
        /// <summary>
        /// Task title (required)
        /// </summary>
        public string Title { get; set; }
        
        /// <summary>
        /// Task type: "task", "subtask", "bug", "feature", "improvement"
        /// </summary>
        public string TaskType { get; set; } = "task";
        
        /// <summary>
        /// Person responsible for the task
        /// </summary>
        public string Owner { get; set; }
        
        /// <summary>
        /// Customer or project associated with task
        /// </summary>
        public string Customer { get; set; }
        
        /// <summary>
        /// Detailed task description
        /// </summary>
        public string Description { get; set; }
        
        /// <summary>
        /// Tags as JSON array: ["tag1", "tag2"]
        /// </summary>
        public string TagsJson { get; set; }
        
        /// <summary>
        /// Primary column ID/name (e.g., "todo", "in-progress", "done")
        /// </summary>
        public string ColumnName { get; set; }
        
        /// <summary>
        /// Sub-column ID/name within column (nullable)
        /// </summary>
        public string SubColumnName { get; set; }
        
        /// <summary>
        /// Whether task is archived
        /// </summary>
        public bool IsArchived { get; set; } = false;
        
        /// <summary>
        /// User ID who owns this task (foreign key)
        /// </summary>
        public string UserId { get; set; }
        
        /// <summary>
        /// UTC timestamp when task was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// UTC timestamp when task was last modified
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        /// <summary>
        /// Reference to the User who owns this task
        /// </summary>
        public virtual User User { get; set; }
        
        // Helper properties (not mapped to DB)
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public string[] Tags
        {
            get => string.IsNullOrEmpty(TagsJson) 
                ? Array.Empty<string>() 
                : JsonSerializer.Deserialize<string[]>(TagsJson);
            set => TagsJson = JsonSerializer.Serialize(value ?? Array.Empty<string>());
        }
    }
}
```

### BoardConfig Entity

```csharp
namespace PrKanban.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json;
    
    /// <summary>
    /// Represents board layout and configuration (1:1 with User)
    /// </summary>
    public class BoardConfig
    {
        /// <summary>
        /// Unique identifier ("config_<guid>")
        /// </summary>
        public string Id { get; set; } = $"config_{Guid.NewGuid()}";
        
        /// <summary>
        /// User ID (foreign key, unique constraint)
        /// </summary>
        public string UserId { get; set; }
        
        /// <summary>
        /// Column order as JSON array: ["todo", "in-progress", "review", "done"]
        /// </summary>
        public string ColumnOrderJson { get; set; }
        
        /// <summary>
        /// Sub-column mapping as JSON: {"todo": ["backlog", "ready"], ...}
        /// </summary>
        public string SubColumnMappingJson { get; set; }
        
        /// <summary>
        /// WIP (Work In Progress) limits as JSON: {"in-progress": 5, "review": 3}
        /// </summary>
        public string WipLimitsJson { get; set; }
        
        /// <summary>
        /// Tags configuration as JSON: {"tag": {"color": "#FF0000", "icon": "flag"}, ...}
        /// </summary>
        public string TagsConfigJson { get; set; }
        
        /// <summary>
        /// Theme preference: "light" or "dark"
        /// </summary>
        public string Theme { get; set; } = "light";
        
        /// <summary>
        /// UTC timestamp when config was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// UTC timestamp when config was last modified
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        /// <summary>
        /// Reference to the User owning this configuration
        /// </summary>
        public virtual User User { get; set; }
        
        // Helper properties (not mapped to DB)
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public string[] ColumnOrder
        {
            get => string.IsNullOrEmpty(ColumnOrderJson) 
                ? Array.Empty<string>() 
                : JsonSerializer.Deserialize<string[]>(ColumnOrderJson);
            set => ColumnOrderJson = JsonSerializer.Serialize(value ?? Array.Empty<string>());
        }
        
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public Dictionary<string, string[]> SubColumnMapping
        {
            get => string.IsNullOrEmpty(SubColumnMappingJson) 
                ? new Dictionary<string, string[]>() 
                : JsonSerializer.Deserialize<Dictionary<string, string[]>>(SubColumnMappingJson);
            set => SubColumnMappingJson = JsonSerializer.Serialize(value ?? new Dictionary<string, string[]>());
        }
        
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public Dictionary<string, int> WipLimits
        {
            get => string.IsNullOrEmpty(WipLimitsJson) 
                ? new Dictionary<string, int>() 
                : JsonSerializer.Deserialize<Dictionary<string, int>>(WipLimitsJson);
            set => WipLimitsJson = JsonSerializer.Serialize(value ?? new Dictionary<string, int>());
        }
    }
}
```

---

## 3. DATABASE CONTEXT (DbContext)

```csharp
namespace PrKanban.Data
{
    using Microsoft.EntityFrameworkCore;
    using PrKanban.Models;
    using System;
    
    /// <summary>
    /// EF Core DbContext for prKanban database
    /// </summary>
    public class PrKanbanContext : DbContext
    {
        /// <summary>
        /// DbSets for database entities
        /// </summary>
        public DbSet<User> Users { get; set; }
        public DbSet<Task> Tasks { get; set; }
        public DbSet<BoardConfig> BoardConfigs { get; set; }
        
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
            
            // User configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(256);
                entity.Property(e => e.Name).HasMaxLength(256);
                entity.Property(e => e.Provider).IsRequired().HasMaxLength(50);
                entity.Property(e => e.ProviderId).IsRequired().HasMaxLength(500);
                entity.Property(e => e.ProfilePicture).HasMaxLength(2000);
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.UpdatedAt).IsRequired();
                
                // Unique constraint on Email and Provider
                entity.HasIndex(e => new { e.Email, e.Provider }).IsUnique();
                
                // One-to-many: User -> Tasks
                entity.HasMany(e => e.Tasks)
                    .WithOne(t => t.User)
                    .HasForeignKey(t => t.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                
                // One-to-one: User -> BoardConfig
                entity.HasOne(e => e.BoardConfig)
                    .WithOne(bc => bc.User)
                    .HasForeignKey<BoardConfig>(bc => bc.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
            
            // Task configuration
            modelBuilder.Entity<Task>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(255);
                entity.Property(e => e.TaskType).HasMaxLength(50);
                entity.Property(e => e.Owner).HasMaxLength(256);
                entity.Property(e => e.Customer).HasMaxLength(256);
                entity.Property(e => e.Description);
                entity.Property(e => e.TagsJson);
                entity.Property(e => e.ColumnName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.SubColumnName).HasMaxLength(100);
                entity.Property(e => e.IsArchived).HasDefaultValue(false);
                entity.Property(e => e.UserId).IsRequired();
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.UpdatedAt).IsRequired();
                
                // Indexes for common queries
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.ColumnName);
                entity.HasIndex(e => new { e.UserId, e.IsArchived });
                entity.HasIndex(e => e.CreatedAt);
                entity.HasIndex(e => e.UpdatedAt);
            });
            
            // BoardConfig configuration
            modelBuilder.Entity<BoardConfig>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.UserId).IsRequired();
                entity.Property(e => e.ColumnOrderJson);
                entity.Property(e => e.SubColumnMappingJson);
                entity.Property(e => e.WipLimitsJson);
                entity.Property(e => e.TagsConfigJson);
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
        public override async System.Threading.Tasks.Task<int> SaveChangesAsync(
            System.Threading.CancellationToken cancellationToken = default)
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
                .Where(e => e.Entity is User or Task or BoardConfig);
            
            foreach (var entry in entries)
            {
                if (entry.State == EntityState.Modified)
                {
                    var entity = entry.Entity;
                    if (entity is User user)
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
```

---

## 4. DATABASE SCHEMA (SQL DDL)

When migrations are run, EF Core generates SQL like this:

```sql
-- Users table
CREATE TABLE "Users" (
    "Id" TEXT NOT NULL PRIMARY KEY,
    "Email" TEXT NOT NULL,
    "Name" TEXT,
    "Provider" TEXT NOT NULL,
    "ProviderId" TEXT NOT NULL,
    "ProfilePicture" TEXT,
    "CreatedAt" TEXT NOT NULL,
    "UpdatedAt" TEXT NOT NULL,
    "LastLoginAt" TEXT
);

CREATE UNIQUE INDEX "IX_Users_Email_Provider" ON "Users" ("Email", "Provider");

-- Tasks table
CREATE TABLE "Tasks" (
    "Id" TEXT NOT NULL PRIMARY KEY,
    "Title" TEXT NOT NULL,
    "TaskType" TEXT,
    "Owner" TEXT,
    "Customer" TEXT,
    "Description" TEXT,
    "TagsJson" TEXT,
    "ColumnName" TEXT NOT NULL,
    "SubColumnName" TEXT,
    "IsArchived" INTEGER NOT NULL DEFAULT 0,
    "UserId" TEXT NOT NULL,
    "CreatedAt" TEXT NOT NULL,
    "UpdatedAt" TEXT NOT NULL,
    FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_Tasks_UserId" ON "Tasks" ("UserId");
CREATE INDEX "IX_Tasks_ColumnName" ON "Tasks" ("ColumnName");
CREATE INDEX "IX_Tasks_UserId_IsArchived" ON "Tasks" ("UserId", "IsArchived");
CREATE INDEX "IX_Tasks_CreatedAt" ON "Tasks" ("CreatedAt");
CREATE INDEX "IX_Tasks_UpdatedAt" ON "Tasks" ("UpdatedAt");

-- BoardConfig table
CREATE TABLE "BoardConfig" (
    "Id" TEXT NOT NULL PRIMARY KEY,
    "UserId" TEXT NOT NULL UNIQUE,
    "ColumnOrderJson" TEXT,
    "SubColumnMappingJson" TEXT,
    "WipLimitsJson" TEXT,
    "TagsConfigJson" TEXT,
    "Theme" TEXT NOT NULL DEFAULT 'light',
    "CreatedAt" TEXT NOT NULL,
    "UpdatedAt" TEXT NOT NULL,
    FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);

-- Enable foreign keys (SQLite requires explicit enabling)
PRAGMA foreign_keys = ON;
```

---

## 5. INDEXES STRATEGY

### Purpose of Each Index

| Index | Table | Columns | Purpose | Query |
|---|---|---|---|---|
| `IX_Users_Email_Provider` | Users | Email, Provider | Unique constraint, fast login lookup | `WHERE Email = ? AND Provider = ?` |
| `IX_Tasks_UserId` | Tasks | UserId | Find all tasks for user | `WHERE UserId = ?` |
| `IX_Tasks_ColumnName` | Tasks | ColumnName | Find tasks in column | `WHERE ColumnName = ?` |
| `IX_Tasks_UserId_IsArchived` | Tasks | UserId, IsArchived | Active tasks for user | `WHERE UserId = ? AND IsArchived = 0` |
| `IX_Tasks_CreatedAt` | Tasks | CreatedAt | Sort by creation date | `ORDER BY CreatedAt DESC` |
| `IX_Tasks_UpdatedAt` | Tasks | UpdatedAt | Sort by modification | `ORDER BY UpdatedAt DESC` |
| `IX_BoardConfig_UserId` | BoardConfig | UserId | One config per user | Unique constraint |

---

## 6. DATA TYPE MAPPINGS

### .NET Types → SQLite Types

| EF Core Type | .NET Type | SQLite Type | Size | Notes |
|---|---|---|---|---|
| string | string | TEXT | ∞ | Max length enforced by EF |
| int | int | INTEGER | 4 bytes | Rarely used in this schema |
| bool | bool | INTEGER (0/1) | 1 byte | SQLite uses 0/1 for boolean |
| DateTime | DateTime | TEXT (ISO 8601) | Variable | Stored as YYYY-MM-DD HH:MM:SS.fff |
| Guid | Guid | TEXT (UUID format) | 36 bytes | Stored as "550e8400-e29b-41d4-a716-446655440000" |

### Example Values

```json
{
  "user": {
    "Id": "user_550e8400-e29b-41d4-a716-446655440000",
    "Email": "john@example.com",
    "Provider": "Google",
    "CreatedAt": "2026-11-14T00:30:00.000Z"
  },
  "task": {
    "Id": "task_a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "Title": "Build login form",
    "Tags": ["frontend", "urgent"],
    "ColumnName": "in-progress",
    "CreatedAt": "2026-11-13T14:45:30.500Z"
  },
  "boardConfig": {
    "ColumnOrder": ["backlog", "todo", "in-progress", "review", "done"],
    "WipLimits": {"in-progress": 5, "review": 3}
  }
}
```

---

## 7. MIGRATIONS STRATEGY

### EF Core Migrations (Phase 3)

**Initial Migration (001_InitialCreate)**:

```bash
cd src/PrKanban
dotnet ef migrations add InitialCreate
dotnet ef database update
```

**Generated Files**:
```
Migrations/
├── 20261114_InitialCreate.cs           (Up migration)
├── 20261114_InitialCreate.Designer.cs  (Metadata)
└── PrKanbanContextModelSnapshot.cs      (Schema snapshot)
```

**Subsequent Migrations** (if schema changes):

```bash
dotnet ef migrations add AddAuditLog
dotnet ef database update
```

---

## 8. DATA POPULATION

### Default Data (Seed)

```csharp
// Optional: Seed initial board config columns if needed
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // ... existing configuration ...
    
    // Seed default column structure (optional)
    modelBuilder.Entity<BoardConfig>()
        .HasData(new BoardConfig
        {
            Id = "config_system",
            UserId = "user_system",
            ColumnOrderJson = JsonSerializer.Serialize(
                new[] { "backlog", "todo", "in-progress", "review", "done" }),
            Theme = "light"
        });
}
```

---

## 9. PERFORMANCE CONSIDERATIONS

### Query Optimization Examples

```csharp
// ❌ BAD: N+1 query problem
var tasks = dbContext.Tasks.Where(t => t.UserId == userId).ToList();
// This loads ALL tasks, then N separate queries for Users

// ✅ GOOD: Include related data
var tasks = dbContext.Tasks
    .Where(t => t.UserId == userId && !t.IsArchived)
    .Include(t => t.User)
    .OrderByDescending(t => t.UpdatedAt)
    .ToList();

// ❌ BAD: Lazy evaluation after materialize
var taskCount = dbContext.Tasks
    .Where(t => t.UserId == userId)
    .ToList()  // Materialize first
    .Count(t => t.Title.Contains("urgent"));  // Filter in memory

// ✅ GOOD: Filter in database
var taskCount = dbContext.Tasks
    .Where(t => t.UserId == userId && t.Title.Contains("urgent"))
    .Count();  // Count in database
```

### Connection Pooling

```csharp
// In appsettings.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=D:\\Apps\\prKanban\\Data\\prkanban.db;Mode=ReadWrite;Cache=Shared;Journal Mode=Wal;Busy Timeout=5000;"
  }
}

// In Startup.cs
services.AddDbContext<PrKanbanContext>(options =>
{
    var connectionString = Configuration.GetConnectionString("DefaultConnection");
    options.UseSqlite(connectionString);
});

// EF Core default pool size: 25 connections
// Suitable for Windows PC with occasional concurrent users
```

---

## 10. BACKUP & RECOVERY

### Database Backup

```powershell
# Manual backup
$timestamp = Get-Date -Format "yyyy-MM-dd_HHmmss"
Copy-Item `
  "D:\Apps\prKanban\Data\prkanban.db" `
  "D:\Apps\prKanban\Data\backups\prkanban_$timestamp.db"
```

### Database Restore

```powershell
# Restore from backup
Stop-Process -Name "prkanban"
Copy-Item `
  "D:\Apps\prKanban\Data\backups\prkanban_2026-11-14_120000.db" `
  "D:\Apps\prKanban\Data\prkanban.db" -Force
Start-Service prkanban
```

### Automatic Backups (Phase 6)

Implement scheduled task:
- Daily backup at 2 AM
- Keep last 7 days of backups
- Store to external drive if possible

---

## 11. DATA VALIDATION

### Entity Validation (EF Core Data Annotations)

```csharp
public class Task
{
    [Required]
    [MaxLength(255)]
    public string Title { get; set; }
    
    [MaxLength(256)]
    public string Owner { get; set; }
    
    [Range(typeof(DateTime), "2020-01-01", "2099-12-31")]
    public DateTime CreatedAt { get; set; }
}
```

### Custom Validation (Business Logic)

```csharp
// In TaskService (Phase 3)
public async Task<Result<TaskDto>> CreateTaskAsync(CreateTaskRequest request)
{
    // Validate title is not empty
    if (string.IsNullOrWhiteSpace(request.Title))
        return Result<TaskDto>.Failure("Title is required");
    
    // Validate column exists
    var boardConfig = await dbContext.BoardConfigs
        .FirstOrDefaultAsync(bc => bc.UserId == request.UserId);
    
    if (!boardConfig.ColumnOrder.Contains(request.ColumnName))
        return Result<TaskDto>.Failure("Invalid column name");
    
    // All valid, proceed with creation
    var task = new Task
    {
        Title = request.Title,
        ColumnName = request.ColumnName,
        UserId = request.UserId
    };
    
    dbContext.Tasks.Add(task);
    await dbContext.SaveChangesAsync();
    
    return Result<TaskDto>.Success(MapToDto(task));
}
```

---

## 12. CONCURRENT ACCESS HANDLING

### SQLite Locking Behavior

```
Timeline:
T1: User A reads task list
T2: User B reads task list (concurrent, allowed)
T3: User A starts update transaction
T4: User B tries to read board config (waits for T3 to complete)
T5: User A commits transaction
T6: User B's read completes
```

### Optimistic Concurrency Control (Optional, Phase 3+)

Add a `Timestamp` column for detecting concurrent modifications:

```csharp
public class Task
{
    [Timestamp]  // EF Core rowversion
    public byte[] RowVersion { get; set; }
    
    // ... other properties
}

// On update, if RowVersion changed, throw ConcurrencyException
try
{
    dbContext.Tasks.Update(task);
    await dbContext.SaveChangesAsync();
}
catch (DbUpdateConcurrencyException)
{
    // Another user modified this task, handle conflict
    return Result.Conflict("Task was modified by another user");
}
```

---

## 13. TESTING STRATEGY

### Database Testing (Phase 3)

```csharp
// Unit test with in-memory SQLite
[TestClass]
public class TaskRepositoryTests
{
    private PrKanbanContext _context;
    
    [TestInitialize]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<PrKanbanContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        
        _context = new PrKanbanContext(options);
        _context.Database.EnsureCreated();
    }
    
    [TestMethod]
    public async Task CreateTask_ShouldAddTaskToDatabase()
    {
        // Arrange
        var userId = "user_test";
        var task = new Task
        {
            Title = "Test task",
            UserId = userId,
            ColumnName = "todo"
        };
        
        // Act
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();
        
        // Assert
        var savedTask = await _context.Tasks
            .FirstOrDefaultAsync(t => t.Id == task.Id);
        Assert.IsNotNull(savedTask);
        Assert.AreEqual("Test task", savedTask.Title);
    }
}
```

---

## 14. MIGRATION FROM FIREBASE

### Data Transformation

```csharp
// Firestore document → EF Core entity
public class FirestoreToSqliteMapper
{
    public static User MapFirebaseUserToSqlite(FirebaseUser fbUser)
    {
        return new User
        {
            Id = fbUser.Uid,  // Keep Firebase UID for now
            Email = fbUser.Email,
            Name = fbUser.DisplayName ?? "Unknown",
            Provider = "Firebase",  // Marker for migration
            ProviderId = fbUser.Uid,
            CreatedAt = DateTime.UtcNow
        };
    }
    
    public static Task MapFirebaseTaskToSqlite(
        FirebaseTask fbTask, 
        string newUserId)
    {
        return new Task
        {
            Id = fbTask.Id,
            Title = fbTask.Title,
            Owner = fbTask.Owner,
            ColumnName = fbTask.Column,
            UserId = newUserId,  // Map to new OAuth user
            CreatedAt = fbTask.CreatedAt,
            UpdatedAt = fbTask.UpdatedAt
        };
    }
}
```

---

## 15. SUCCESS CRITERIA FOR DATABASE PHASE

✅ **Models defined** (User, Task, BoardConfig)  
✅ **DbContext configured** (relationships, constraints)  
✅ **Schema designed** (tables, indexes, constraints)  
✅ **Migrations ready** (Phase 3 implementation)  
✅ **Performance optimized** (indexes, queries)  
✅ **Testing strategy** (unit tests, data validation)  
✅ **Documentation complete** (this file)  

---

## 16. FILE LOCATIONS (Phase 3 Implementation)

```
PrKanban.Backend/
├── Models/
│   ├── User.cs
│   ├── Task.cs
│   └── BoardConfig.cs
├── Data/
│   ├── PrKanbanContext.cs
│   └── Migrations/
│       └── 20261114_InitialCreate.cs
├── Services/
│   ├── TaskService.cs
│   ├── BoardConfigService.cs
│   └── UserService.cs
└── Repositories/
    ├── TaskRepository.cs
    └── BoardConfigRepository.cs
```

---

**Document Status**: ✅ DATABASE DESIGN COMPLETE

Next Phase 3: Implement models, DbContext, and migrations
