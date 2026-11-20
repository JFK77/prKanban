namespace PrKanban.Models;

public sealed class TaskStatusEvent
{
    public long Id { get; set; }
    public string TaskId { get; set; } = "";
    public string ColumnName { get; set; } = "";
    public string? SubColumnName { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public bool IsBaseline { get; set; }
}
