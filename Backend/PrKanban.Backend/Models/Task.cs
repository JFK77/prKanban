using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrKanban.Models
{
    /// <summary>
    /// Represents a single task/card on the Kanban board
    /// </summary>
    public class Task
    {
        /// <summary>
        /// Unique identifier for task
        /// </summary>
        public string Id { get; set; } = $"task_{Guid.NewGuid()}";

        /// <summary>
        /// Task title (required)
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Task type (task, subtask, bug, feature, etc.)
        /// </summary>
        public string? TaskType { get; set; }

        /// <summary>
        /// Person assigned to the task; does not determine access rights.
        /// </summary>
        public string? Owner { get; set; }

        /// <summary>
        /// Customer or project associated with task
        /// </summary>
        public string? Customer { get; set; }

        /// <summary>
        /// Detailed task description
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Tags as JSON array ["tag1", "tag2"]
        /// </summary>
        [JsonIgnore]
        public string? TagsJson { get; set; }

        /// <summary>
        /// Column ID/name (e.g., "todo", "in-progress", "done")
        /// </summary>
        public string ColumnName { get; set; } = "todo";

        /// <summary>
        /// Sub-column ID/name within column (nullable)
        /// </summary>
        public string? SubColumnName { get; set; }

        /// <summary>
        /// Saved position within the column and sub-column
        /// </summary>
        [NotMapped]
        public double? Order => Ordering?.Value;

        [JsonIgnore]
        public virtual TaskOrder? Ordering { get; set; }

        /// <summary>
        /// Whether task is archived (soft delete)
        /// </summary>
        public bool IsArchived { get; set; } = false;

        /// <summary>
        /// User ID of the account that created this task (foreign key).
        /// </summary>
        public string UserId { get; set; } = string.Empty;

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
        public virtual ApplicationUser? User { get; set; }

        // Non-mapped property for tags array
        /// <summary>
        /// Tags as string array (for API serialization)
        /// </summary>
        [JsonPropertyName("tags")]
        public string[] Tags
        {
            get
            {
                if (string.IsNullOrEmpty(TagsJson))
                    return Array.Empty<string>();
                try
                {
                    return System.Text.Json.JsonSerializer.Deserialize<string[]>(TagsJson) ?? Array.Empty<string>();
                }
                catch
                {
                    return Array.Empty<string>();
                }
            }
            set
            {
                TagsJson = System.Text.Json.JsonSerializer.Serialize(value ?? Array.Empty<string>());
            }
        }
    }
}
