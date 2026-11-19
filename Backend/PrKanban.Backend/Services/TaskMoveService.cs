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
        if (!MasterBoardColumns.IsValid(columnName) ||
            !IsValidSubColumn(columnName, subColumnName) ||
            beforeTaskId == taskId)
            return TaskMoveResult.InvalidTarget;

        return await MoveCoreAsync(taskId, columnName, subColumnName, beforeTaskId, null, cancellationToken);
    }

    public async Task<TaskMoveResult> MoveToProjectColumnAsync(
        string taskId, string projectBoardColumnId, string? beforeTaskId, string? subColumnName,
        CancellationToken cancellationToken)
    {
        if (beforeTaskId == taskId)
            return TaskMoveResult.InvalidTarget;

        var target = await database.ProjectBoardColumns
            .AsNoTracking()
            .FirstOrDefaultAsync(column => column.Id == projectBoardColumnId, cancellationToken);
        if (target is null || !MasterBoardColumns.IsValid(target.MasterColumnName))
            return TaskMoveResult.InvalidTarget;
        if (subColumnName is not null && !IsValidSubColumn(target.MasterColumnName, subColumnName))
            return TaskMoveResult.InvalidTarget;

        var task = await database.Tasks
            .Include(item => item.DeliverableAssignment)
            .ThenInclude(assignment => assignment!.Deliverable)
            .FirstOrDefaultAsync(item => item.Id == taskId, cancellationToken);
        if (task is null)
            return TaskMoveResult.TaskNotFound;
        if (task.DeliverableAssignment?.Deliverable?.ProjectId != target.ProjectId)
            return TaskMoveResult.InvalidTarget;

        var subColumn = subColumnName ?? (target.MasterColumnName == MasterBoardColumns.Backlog
            ? MasterBoardColumns.Backlog
            : task.SubColumnName is "Doing" or "Done" ? task.SubColumnName : "Doing");
        return await MoveCoreAsync(
            taskId, target.MasterColumnName, subColumn, beforeTaskId, target.Id, cancellationToken);
    }

    private async Task<TaskMoveResult> MoveCoreAsync(
        string taskId,
        string columnName,
        string subColumnName,
        string? beforeTaskId,
        string? projectBoardColumnId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
        var movedTask = await database.Tasks
            .Include(task => task.Ordering)
            .Include(task => task.DeliverableAssignment)
            .ThenInclude(assignment => assignment!.Deliverable)
            .Include(task => task.ProjectBoardPosition)
            .SingleOrDefaultAsync(task => task.Id == taskId, cancellationToken);
        if (movedTask is null)
            return TaskMoveResult.TaskNotFound;

        var targetColumn = projectBoardColumnId is null
            ? null
            : await database.ProjectBoardColumns.SingleOrDefaultAsync(
                column => column.Id == projectBoardColumnId, cancellationToken);
        if (projectBoardColumnId is not null &&
            (targetColumn is null ||
             targetColumn.ProjectId != movedTask.DeliverableAssignment?.Deliverable?.ProjectId))
            return TaskMoveResult.InvalidTarget;

        var currentColumnName = MasterBoardColumns.Normalize(movedTask.ColumnName);
        var changesMasterColumn = currentColumnName != columnName;
        var projectId = movedTask.DeliverableAssignment?.Deliverable?.ProjectId;
        var targetProjectColumn = targetColumn;
        if (projectBoardColumnId is null && changesMasterColumn && projectId is not null)
        {
            targetProjectColumn = await database.ProjectBoardColumns
                .Where(column => column.ProjectId == projectId && column.MasterColumnName == columnName)
                .OrderBy(column => column.SortOrder).ThenBy(column => column.Id)
                .FirstOrDefaultAsync(cancellationToken);
            if (targetProjectColumn is null)
                return TaskMoveResult.InvalidTarget;
        }

        if (projectBoardColumnId is not null)
        {
            if (changesMasterColumn || movedTask.SubColumnName != subColumnName)
            {
                var masterLaneResult = await ReorderMasterLaneAsync(
                    movedTask, columnName, subColumnName, null, cancellationToken);
                if (masterLaneResult != TaskMoveResult.Success) return masterLaneResult;
            }
        }
        else
        {
            var masterLaneResult = await ReorderMasterLaneAsync(
                movedTask, columnName, subColumnName, beforeTaskId, cancellationToken);
            if (masterLaneResult != TaskMoveResult.Success) return masterLaneResult;
        }

        if (targetProjectColumn is not null &&
            (projectBoardColumnId is not null || changesMasterColumn))
        {
            var projectLaneResult = await ReorderProjectLaneAsync(
                movedTask, targetProjectColumn.Id,
                projectBoardColumnId is null ? null : beforeTaskId,
                cancellationToken);
            if (projectLaneResult != TaskMoveResult.Success) return projectLaneResult;
        }

        await database.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return TaskMoveResult.Success;
    }

    private async Task<TaskMoveResult> ReorderMasterLaneAsync(
        PrKanban.Models.Task movedTask, string columnName, string subColumnName, string? beforeTaskId,
        CancellationToken cancellationToken)
    {
        var laneTasks = await database.Tasks.Include(task => task.Ordering)
            .Where(task => task.Id != movedTask.Id
                && EF.Functions.Collate(task.ColumnName == "" ? "ToDo" : task.ColumnName, "NOCASE") == columnName
                && (columnName == MasterBoardColumns.Backlog
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

        return TaskMoveResult.Success;
    }

    private async Task<TaskMoveResult> ReorderProjectLaneAsync(
        PrKanban.Models.Task movedTask, string targetColumnId, string? beforeTaskId,
        CancellationToken cancellationToken)
    {
        var lanePositions = await database.ProjectBoardTaskPositions
            .Where(position => position.ProjectBoardColumnId == targetColumnId && position.TaskId != movedTask.Id)
            .OrderBy(position => position.SortOrder).ThenBy(position => position.TaskId)
            .ToListAsync(cancellationToken);
        var index = beforeTaskId is null
            ? lanePositions.Count
            : lanePositions.FindIndex(position => position.TaskId == beforeTaskId);
        if (index < 0)
            return TaskMoveResult.InvalidBeforeTask;

        var position = movedTask.ProjectBoardPosition;
        if (position is null)
        {
            position = await database.ProjectBoardTaskPositions
                .FirstOrDefaultAsync(item => item.TaskId == movedTask.Id, cancellationToken);
        }
        if (position is null)
        {
            position = new ProjectBoardTaskPosition { TaskId = movedTask.Id };
            database.ProjectBoardTaskPositions.Add(position);
            movedTask.ProjectBoardPosition = position;
        }

        position.ProjectBoardColumnId = targetColumnId;
        lanePositions.Insert(index, position);
        for (var i = 0; i < lanePositions.Count; i++)
            lanePositions[i].SortOrder = (i + 1) * 1024d;
        return TaskMoveResult.Success;
    }

    private static bool IsValidSubColumn(string columnName, string subColumnName) =>
        columnName == MasterBoardColumns.Backlog
            ? subColumnName == MasterBoardColumns.Backlog
            : subColumnName is "Doing" or "Done";
}
