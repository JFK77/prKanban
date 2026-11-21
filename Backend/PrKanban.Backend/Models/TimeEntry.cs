using System.ComponentModel.DataAnnotations.Schema;

namespace PrKanban.Models;

/// <summary>
/// Represents a time tracking entry for a task
/// </summary>
public class TimeEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    
    /// <summary>
    /// Task ID this time entry belongs to
    /// </summary>
    public string TaskId { get; set; } = "";
    
    /// <summary>
    /// User ID who logged this time (the recorder)
    /// </summary>
    public string UserId { get; set; } = "";
    
    /// <summary>
    /// Start date and time (UTC)
    /// </summary>
    public DateTime StartTime { get; set; }
    
    /// <summary>
    /// End date and time (UTC), nullable if duration was entered directly
    /// </summary>
    public DateTime? EndTime { get; set; }
    
    /// <summary>
    /// Duration in hours (can be calculated from StartTime/EndTime or entered directly)
    /// </summary>
    public double DurationHours { get; set; }
    
    /// <summary>
    /// Optional comment/description
    /// </summary>
    public string? Comment { get; set; }
    
    /// <summary>
    /// Whether this time is billable or not (default: true)
    /// </summary>
    public bool IsBillable { get; set; } = true;
    
    /// <summary>
    /// UTC timestamp when this entry was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// UTC timestamp when this entry was last modified
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual Task? Task { get; set; }
    public virtual ApplicationUser? User { get; set; }
}
