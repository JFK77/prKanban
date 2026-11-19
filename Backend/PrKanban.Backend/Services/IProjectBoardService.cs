using System.ComponentModel.DataAnnotations;
using PrKanban.Models;
using Task = System.Threading.Tasks.Task;

namespace PrKanban.Services;

/// <summary>A master-board category that project columns can map to.</summary>
public sealed record MasterBoardColumnResponse(string Name, string Label);
/// <summary>A project-specific column and its master-board mapping.</summary>
public sealed record ProjectBoardColumnResponse(
    string Id, string Name, string MasterColumnName, int SortOrder, bool ShowSubLanes);
/// <summary>A user assigned to manage a project board.</summary>
public sealed record ProjectBoardLeadResponse(string UserId, string Email, string? FullName);
/// <summary>A project board with its configuration and manager permissions.</summary>
public sealed record ProjectBoardResponse(
    string ProjectId,
    string ProjectName,
    bool CanManage,
    IReadOnlyList<ProjectBoardColumnResponse> Columns,
    IReadOnlyList<ProjectBoardLeadResponse> Leads,
    IReadOnlyList<MasterBoardColumnResponse> MasterColumns);

/// <summary>Payload for creating a project-specific board column.</summary>
public sealed record CreateProjectBoardColumnRequest
{
    [Required, MaxLength(100)] public required string Name { get; init; }
    [Required, MaxLength(100)] public required string MasterColumnName { get; init; }
    public bool ShowSubLanes { get; init; }
}

/// <summary>Payload for renaming or remapping an empty project-board column.</summary>
public sealed record UpdateProjectBoardColumnRequest
{
    [Required, MaxLength(100)] public required string Name { get; init; }
    [Required, MaxLength(100)] public required string MasterColumnName { get; init; }
    public bool? ShowSubLanes { get; init; }
}

/// <summary>Payload containing the new order of all project-board columns.</summary>
public sealed record ReorderProjectBoardColumnsRequest
{
    [Required, MinLength(1)] public required IReadOnlyList<string> ColumnIds { get; init; }
}

/// <summary>Payload assigning project-board manager accounts.</summary>
public sealed record SetProjectBoardLeadsRequest
{
    [Required] public required IReadOnlyList<string> UserIds { get; init; }
}

/// <summary>Payload moving a task to a project-board column and optional position.</summary>
public sealed record MoveProjectBoardTaskRequest
{
    [Required, MaxLength(256)] public required string ProjectBoardColumnId { get; init; }
    public string? BeforeTaskId { get; init; }
    [MaxLength(20)] public string? SubColumnName { get; init; }
}

/// <summary>Result category for project-board operations.</summary>
public enum ProjectBoardResultCode
{
    Success,
    NotFound,
    Forbidden,
    Invalid,
    Conflict
}

/// <summary>Outcome and optional data from a project-board operation.</summary>
public sealed record ProjectBoardResult<T>(ProjectBoardResultCode Code, T? Value = default);

/// <summary>Project board persistence and workflow operations.</summary>
public interface IProjectBoardService
{
    Task<ProjectBoardResponse?> GetAsync(string projectId, string userId, bool isAdmin, CancellationToken ct);
    Task<ProjectBoardResult<ProjectBoardColumnResponse>> GetColumnAsync(string projectId, string columnId, CancellationToken ct);
    Task<ProjectBoardResult<ProjectBoardColumnResponse>> CreateColumnAsync(string projectId, string userId, bool isAdmin, CreateProjectBoardColumnRequest request, CancellationToken ct);
    Task<ProjectBoardResult<ProjectBoardColumnResponse>> UpdateColumnAsync(string projectId, string columnId, string userId, bool isAdmin, UpdateProjectBoardColumnRequest request, CancellationToken ct);
    Task<ProjectBoardResult<bool>> DeleteColumnAsync(string projectId, string columnId, string userId, bool isAdmin, CancellationToken ct);
    Task<ProjectBoardResult<bool>> ReorderColumnsAsync(string projectId, string userId, bool isAdmin, ReorderProjectBoardColumnsRequest request, CancellationToken ct);
    Task<ProjectBoardResult<IReadOnlyList<ProjectBoardLeadResponse>>> SetLeadsAsync(string projectId, SetProjectBoardLeadsRequest request, CancellationToken ct);
    Task<ProjectBoardResult<bool>> MoveTaskAsync(string projectId, string taskId, MoveProjectBoardTaskRequest request, CancellationToken ct);
    Task EnsureDefaultColumnsAsync(string projectId, CancellationToken ct);
    Task EnsureTaskPositionAsync(PrKanban.Models.Task task, string projectId, string masterColumnName, CancellationToken ct);
}
