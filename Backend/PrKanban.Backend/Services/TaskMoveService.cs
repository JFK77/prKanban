using Microsoft.EntityFrameworkCore;
using PrKanban.Data;
using PrKanban.Models;

namespace PrKanban.Services;

public sealed class TaskMoveService(PrKanbanContext database) : ITaskMoveService
{
    public async Task<TaskMoveResult> MoveAsync(
        string taskId, string columnName, string subColumnName, string? beforeTaskId,
        CancellationToken cancellationToken)
    {
        var validColumn = columnName is "Backlog" or "ToDo" or "Analysis" or "InProgress" or "Done";
        var validSubColumn = columnName == "Backlog"
            ? subColumnName == "Backlog"
            : subColumnName is "Doing" or "Done";
        if (!validColumn || !validSubColumn || beforeTaskId == taskId)
            return TaskMoveResult.InvalidTarget;

        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
        var movedTask = await database.Tasks.Include(task => task.Ordering)
            .SingleOrDefaultAsync(task => task.Id == taskId, cancellationToken);
        if (movedTask is null)
            return TaskMoveResult.TaskNotFound;

        var laneTasks = await database.Tasks.Include(task => task.Ordering)
            .Where(task => task.Id != taskId
                && EF.Functions.Collate(task.ColumnName == "" ? "ToDo" : task.ColumnName, "NOCASE") == columnName
                && (columnName == "Backlog"
                    || task.SubColumnName == subColumnName
                    || (subColumnName == "Doing" && (task.SubColumnName == null || task.SubColumnName == ""))))
            .ToListAsync(cancellationToken);

        laneTasks = laneTasks
            .OrderBy(task => task.Order ?? double.PositiveInfinity)
            .ThenBy(task => task.CreatedAt)
            .ThenBy(task => task.Id, StringComparer.Ordinal)
            .ToList();

        var index = beforeTaskId is null ? laneTasks.Count : laneTasks.FindIndex(task => task.Id == beforeTaskId);
        if (index < 0)
            return TaskMoveResult.InvalidBeforeTask;

        movedTask.ColumnName = columnName;
        movedTask.SubColumnName = subColumnName;
        laneTasks.Insert(index, movedTask);
        for (var i = 0; i < laneTasks.Count; i++)
        {
            var task = laneTasks[i];
            if (task.Ordering is null)
            {
                task.Ordering = new TaskOrder { TaskId = task.Id };
                database.Set<TaskOrder>().Add(task.Ordering);
            }
            task.Ordering.Value = (i + 1) * 1024d;
        }

        await database.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return TaskMoveResult.Success;
    }
}
