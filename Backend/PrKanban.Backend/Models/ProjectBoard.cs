namespace PrKanban.Models;

public sealed class ProjectBoardColumn
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string ProjectId { get; set; } = "";
    public string Name { get; set; } = "";
    public string MasterColumnName { get; set; } = "";
    public int SortOrder { get; set; }
    public bool ShowSubLanes { get; set; }
}

public sealed class ProjectBoardLead
{
    public string ProjectId { get; set; } = "";
    public string UserId { get; set; } = "";
}

public sealed class ProjectBoardTaskPosition
{
    public string TaskId { get; set; } = "";
    public string ProjectBoardColumnId { get; set; } = "";
    public double SortOrder { get; set; }
    public ProjectBoardColumn? Column { get; set; }
}

public static class MasterBoardColumns
{
    public const string Backlog = "Backlog";
    public const string ToDo = "ToDo";
    public const string Analysis = "Analysis";
    public const string InProgress = "InProgress";
    public const string Done = "Done";

    public static readonly IReadOnlyList<(string Name, string Label)> All =
    [
        (Backlog, "Backlog"),
        (ToDo, "To Do"),
        (Analysis, "Analysis"),
        (InProgress, "In Progress"),
        (Done, "Done")
    ];

    public static bool IsValid(string? name) => All.Any(column => column.Name == name);

    public static string Normalize(string? name) => name?.Trim().ToLowerInvariant() switch
    {
        "backlog" => Backlog,
        "todo" or "to do" => ToDo,
        "analysis" => Analysis,
        "inprogress" or "in progress" => InProgress,
        "done" => Done,
        _ => ToDo
    };
}
