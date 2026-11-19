namespace PrKanban.Services;

public enum TaskMoveResult
{
    Success,
    TaskNotFound,
    InvalidTarget,
    InvalidBeforeTask
}

public interface ITaskMoveService
{
    Task<TaskMoveResult> MoveAsync(
        string taskId, string columnName, string subColumnName, string? beforeTaskId,
        CancellationToken cancellationToken);

    Task<TaskMoveResult> MoveToProjectColumnAsync(
        string taskId, string projectBoardColumnId, string? beforeTaskId, string? subColumnName,
        CancellationToken cancellationToken);
}
