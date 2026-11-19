using Microsoft.EntityFrameworkCore;
using PrKanban.Data;
using PrKanban.Models;
using Task = System.Threading.Tasks.Task;

namespace PrKanban.Services;

public sealed class ProjectBoardService(PrKanbanContext db, ITaskMoveService taskMoves) : IProjectBoardService
{
    public async Task<ProjectBoardResponse?> GetAsync(string projectId, string userId, bool isAdmin, CancellationToken ct)
    {
        var project = await db.Projects.AsNoTracking().FirstOrDefaultAsync(x => x.Id == projectId, ct);
        if (project is null) return null;

        var columns = await db.ProjectBoardColumns.AsNoTracking()
            .Where(x => x.ProjectId == projectId)
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Id)
            .Select(x => new ProjectBoardColumnResponse(
                x.Id, x.Name, x.MasterColumnName, x.SortOrder, x.ShowSubLanes))
            .ToListAsync(ct);
        var leads = await (
            from lead in db.ProjectBoardLeads.AsNoTracking()
            join user in db.Users.AsNoTracking() on lead.UserId equals user.Id
            where lead.ProjectId == projectId
            orderby user.Email
            select new ProjectBoardLeadResponse(user.Id, user.Email ?? "", user.FullName))
            .ToListAsync(ct);
        var canManage = isAdmin || leads.Any(x => x.UserId == userId);

        return new ProjectBoardResponse(
            project.Id,
            project.Name,
            canManage,
            columns,
            canManage ? leads : Array.Empty<ProjectBoardLeadResponse>(),
            MasterBoardColumns.All.Select(x => new MasterBoardColumnResponse(x.Name, x.Label)).ToList());
    }

    public async Task<ProjectBoardResult<ProjectBoardColumnResponse>> GetColumnAsync(string projectId, string columnId, CancellationToken ct)
    {
        var column = await db.ProjectBoardColumns.AsNoTracking()
            .Where(x => x.ProjectId == projectId && x.Id == columnId)
            .Select(x => new ProjectBoardColumnResponse(
                x.Id, x.Name, x.MasterColumnName, x.SortOrder, x.ShowSubLanes))
            .FirstOrDefaultAsync(ct);
        return column is null
            ? new(ProjectBoardResultCode.NotFound)
            : new(ProjectBoardResultCode.Success, column);
    }

    public async Task<ProjectBoardResult<ProjectBoardColumnResponse>> CreateColumnAsync(
        string projectId, string userId, bool isAdmin, CreateProjectBoardColumnRequest request, CancellationToken ct)
    {
        var permission = await CheckManageAccessAsync(projectId, userId, isAdmin, ct);
        if (permission != ProjectBoardResultCode.Success)
            return new(permission);
        if (string.IsNullOrWhiteSpace(request.Name) ||
            !MasterBoardColumns.IsValid(request.MasterColumnName) ||
            (request.ShowSubLanes && request.MasterColumnName == MasterBoardColumns.Backlog))
            return new(ProjectBoardResultCode.Invalid);

        var sortOrder = await NextColumnOrderAsync(projectId, ct);
        var column = new ProjectBoardColumn
        {
            ProjectId = projectId,
            Name = request.Name.Trim(),
            MasterColumnName = request.MasterColumnName,
            SortOrder = sortOrder,
            ShowSubLanes = request.ShowSubLanes
        };
        db.ProjectBoardColumns.Add(column);
        await db.SaveChangesAsync(ct);
        return new(ProjectBoardResultCode.Success, Map(column));
    }

    public async Task<ProjectBoardResult<ProjectBoardColumnResponse>> UpdateColumnAsync(
        string projectId, string columnId, string userId, bool isAdmin, UpdateProjectBoardColumnRequest request, CancellationToken ct)
    {
        var permission = await CheckManageAccessAsync(projectId, userId, isAdmin, ct);
        if (permission != ProjectBoardResultCode.Success)
            return new(permission);
        if (string.IsNullOrWhiteSpace(request.Name) ||
            !MasterBoardColumns.IsValid(request.MasterColumnName) ||
            (request.ShowSubLanes == true && request.MasterColumnName == MasterBoardColumns.Backlog))
            return new(ProjectBoardResultCode.Invalid);

        var column = await db.ProjectBoardColumns.FirstOrDefaultAsync(x => x.ProjectId == projectId && x.Id == columnId, ct);
        if (column is null) return new(ProjectBoardResultCode.NotFound);
        var showSubLanes = request.ShowSubLanes ?? column.ShowSubLanes;
        if (request.MasterColumnName == MasterBoardColumns.Backlog && showSubLanes)
            return new(ProjectBoardResultCode.Invalid);
        if (column.MasterColumnName != request.MasterColumnName)
        {
            var hasTasks = await db.ProjectBoardTaskPositions.AnyAsync(x => x.ProjectBoardColumnId == column.Id, ct);
            var isLastMappedColumn = !await db.ProjectBoardColumns.AnyAsync(
                x => x.ProjectId == projectId && x.MasterColumnName == column.MasterColumnName && x.Id != column.Id, ct);
            if (hasTasks || isLastMappedColumn) return new(ProjectBoardResultCode.Conflict);
        }

        column.Name = request.Name.Trim();
        column.MasterColumnName = request.MasterColumnName;
        column.ShowSubLanes = showSubLanes;
        await db.SaveChangesAsync(ct);
        return new(ProjectBoardResultCode.Success, Map(column));
    }

    public async Task<ProjectBoardResult<bool>> DeleteColumnAsync(
        string projectId, string columnId, string userId, bool isAdmin, CancellationToken ct)
    {
        var permission = await CheckManageAccessAsync(projectId, userId, isAdmin, ct);
        if (permission != ProjectBoardResultCode.Success)
            return new(permission);

        var column = await db.ProjectBoardColumns.FirstOrDefaultAsync(x => x.ProjectId == projectId && x.Id == columnId, ct);
        if (column is null) return new(ProjectBoardResultCode.NotFound);
        var hasTasks = await db.ProjectBoardTaskPositions.AnyAsync(x => x.ProjectBoardColumnId == column.Id, ct);
        var isLastMappedColumn = !await db.ProjectBoardColumns.AnyAsync(
            x => x.ProjectId == projectId && x.MasterColumnName == column.MasterColumnName && x.Id != column.Id, ct);
        if (hasTasks || isLastMappedColumn) return new(ProjectBoardResultCode.Conflict);

        db.ProjectBoardColumns.Remove(column);
        await db.SaveChangesAsync(ct);
        return new(ProjectBoardResultCode.Success, true);
    }

    public async Task<ProjectBoardResult<bool>> ReorderColumnsAsync(
        string projectId, string userId, bool isAdmin, ReorderProjectBoardColumnsRequest request, CancellationToken ct)
    {
        var permission = await CheckManageAccessAsync(projectId, userId, isAdmin, ct);
        if (permission != ProjectBoardResultCode.Success)
            return new(permission);

        var columns = await db.ProjectBoardColumns.Where(x => x.ProjectId == projectId).ToListAsync(ct);
        if (request.ColumnIds.Count != columns.Count ||
            request.ColumnIds.Distinct(StringComparer.Ordinal).Count() != columns.Count)
            return new(ProjectBoardResultCode.Invalid);

        var byId = columns.ToDictionary(x => x.Id, StringComparer.Ordinal);
        if (request.ColumnIds.Any(id => !byId.ContainsKey(id)))
            return new(ProjectBoardResultCode.Invalid);
        for (var index = 0; index < request.ColumnIds.Count; index++)
            byId[request.ColumnIds[index]].SortOrder = index;
        await db.SaveChangesAsync(ct);
        return new(ProjectBoardResultCode.Success, true);
    }

    public async Task<ProjectBoardResult<IReadOnlyList<ProjectBoardLeadResponse>>> SetLeadsAsync(
        string projectId, SetProjectBoardLeadsRequest request, CancellationToken ct)
    {
        if (!await db.Projects.AnyAsync(x => x.Id == projectId, ct))
            return new(ProjectBoardResultCode.NotFound);
        var userIds = request.UserIds.Distinct(StringComparer.Ordinal).ToList();
        if (userIds.Count != request.UserIds.Count)
            return new(ProjectBoardResultCode.Invalid);
        var users = await db.Users.Where(x => userIds.Contains(x.Id))
            .Select(x => new ProjectBoardLeadResponse(x.Id, x.Email ?? "", x.FullName))
            .ToListAsync(ct);
        if (users.Count != userIds.Count)
            return new(ProjectBoardResultCode.Invalid);

        var existing = await db.ProjectBoardLeads.Where(x => x.ProjectId == projectId).ToListAsync(ct);
        var requestedUserIds = userIds.ToHashSet(StringComparer.Ordinal);
        db.ProjectBoardLeads.RemoveRange(existing.Where(lead => !requestedUserIds.Contains(lead.UserId)));
        var existingUserIds = existing.Select(lead => lead.UserId).ToHashSet(StringComparer.Ordinal);
        db.ProjectBoardLeads.AddRange(userIds
            .Where(userId => !existingUserIds.Contains(userId))
            .Select(userId => new ProjectBoardLead { ProjectId = projectId, UserId = userId }));
        await db.SaveChangesAsync(ct);
        return new(ProjectBoardResultCode.Success, users.OrderBy(x => x.Email).ToList());
    }

    public async Task<ProjectBoardResult<bool>> MoveTaskAsync(
        string projectId, string taskId, MoveProjectBoardTaskRequest request, CancellationToken ct)
    {
        var projectExists = await db.Projects.AnyAsync(x => x.Id == projectId, ct);
        if (!projectExists) return new(ProjectBoardResultCode.NotFound);
        var target = await db.ProjectBoardColumns
            .FirstOrDefaultAsync(x => x.Id == request.ProjectBoardColumnId && x.ProjectId == projectId, ct);
        if (target is null) return new(ProjectBoardResultCode.Invalid);
        if (request.SubColumnName is not null &&
            (!target.ShowSubLanes || target.MasterColumnName == MasterBoardColumns.Backlog))
            return new(ProjectBoardResultCode.Invalid);

        var taskProjectId = await db.TaskDeliverables
            .Where(x => x.TaskId == taskId)
            .Select(x => x.Deliverable.ProjectId)
            .FirstOrDefaultAsync(ct);
        if (taskProjectId is null) return new(ProjectBoardResultCode.NotFound);
        if (taskProjectId != projectId) return new(ProjectBoardResultCode.Invalid);

        var result = await taskMoves.MoveToProjectColumnAsync(
            taskId, target.Id, request.BeforeTaskId, request.SubColumnName, ct);
        return result switch
        {
            TaskMoveResult.Success => new(ProjectBoardResultCode.Success, true),
            TaskMoveResult.TaskNotFound => new(ProjectBoardResultCode.NotFound),
            TaskMoveResult.InvalidBeforeTask => new(ProjectBoardResultCode.Conflict),
            _ => new(ProjectBoardResultCode.Invalid)
        };
    }

    public async Task EnsureDefaultColumnsAsync(string projectId, CancellationToken ct)
    {
        var existingMasterColumns = await db.ProjectBoardColumns
            .Where(x => x.ProjectId == projectId)
            .Select(x => x.MasterColumnName)
            .ToListAsync(ct);
        var next = await NextColumnOrderAsync(projectId, ct);
        foreach (var master in MasterBoardColumns.All)
        {
            if (existingMasterColumns.Contains(master.Name, StringComparer.Ordinal)) continue;
            db.ProjectBoardColumns.Add(new ProjectBoardColumn
            {
                Id = $"default-{projectId}-{master.Name}",
                ProjectId = projectId,
                Name = master.Label,
                MasterColumnName = master.Name,
                SortOrder = next++
            });
        }
    }

    public async Task EnsureTaskPositionAsync(
        PrKanban.Models.Task task, string projectId, string masterColumnName, CancellationToken ct)
    {
        var masterName = MasterBoardColumns.Normalize(masterColumnName);
        await EnsureDefaultColumnsAsync(projectId, ct);
        var targetColumn = db.ProjectBoardColumns.Local
            .Where(x => x.ProjectId == projectId && x.MasterColumnName == masterName)
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Id)
            .FirstOrDefault();
        targetColumn ??= await db.ProjectBoardColumns
            .Where(x => x.ProjectId == projectId && x.MasterColumnName == masterName)
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Id)
            .FirstOrDefaultAsync(ct);
        if (targetColumn is null)
            throw new InvalidOperationException($"Project {projectId} has no column mapped to {masterName}.");

        var position = task.ProjectBoardPosition ??
            await db.ProjectBoardTaskPositions.FirstOrDefaultAsync(x => x.TaskId == task.Id, ct);
        if (position is null)
        {
            position = new ProjectBoardTaskPosition { TaskId = task.Id };
            task.ProjectBoardPosition = position;
            db.ProjectBoardTaskPositions.Add(position);
        }
        position.ProjectBoardColumnId = targetColumn.Id;
        position.SortOrder = await NextTaskOrderAsync(targetColumn.Id, ct);
    }

    private async Task<ProjectBoardResultCode> CheckManageAccessAsync(
        string projectId, string userId, bool isAdmin, CancellationToken ct)
    {
        if (!await db.Projects.AnyAsync(x => x.Id == projectId, ct))
            return ProjectBoardResultCode.NotFound;
        if (isAdmin || await db.ProjectBoardLeads.AnyAsync(x => x.ProjectId == projectId && x.UserId == userId, ct))
            return ProjectBoardResultCode.Success;
        return ProjectBoardResultCode.Forbidden;
    }

    private async Task<int> NextColumnOrderAsync(string projectId, CancellationToken ct) =>
        (await db.ProjectBoardColumns.Where(x => x.ProjectId == projectId)
            .Select(x => (int?)x.SortOrder).MaxAsync(ct) ?? -1) + 1;

    private async Task<double> NextTaskOrderAsync(string columnId, CancellationToken ct)
    {
        var persistedMax = await db.ProjectBoardTaskPositions
            .Where(x => x.ProjectBoardColumnId == columnId)
            .Select(x => (double?)x.SortOrder)
            .MaxAsync(ct) ?? -1024d;
        var trackedMax = db.ProjectBoardTaskPositions.Local
            .Where(x => x.ProjectBoardColumnId == columnId)
            .Select(x => (double?)x.SortOrder)
            .Max() ?? -1024d;
        return Math.Max(persistedMax, trackedMax) + 1024d;
    }

    private static ProjectBoardColumnResponse Map(ProjectBoardColumn column) =>
        new(column.Id, column.Name, column.MasterColumnName, column.SortOrder, column.ShowSubLanes);
}
