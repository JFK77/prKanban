using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PrKanban.Data;
using PrKanban.Models;

namespace PrKanban.Services;

public sealed class ProjectHierarchyService(PrKanbanContext db, IProjectBoardService projectBoards) : IProjectHierarchyService
{
    private static CustomerResponse Map(Customer x) => new(x.Id, x.Name, x.Details?.DebitorNo, x.Details?.CrmUrl);
    private static ProjectResponse Map(Project x) => new(x.Id, x.Name, x.CustomerId, x.Details?.ShortKey, x.Details?.CrmLink, x.Details?.CrmUrl, x.Details?.InternalOrderNumber, x.Details?.CustomerOrderNumber, x.Details?.DefaultAssignee);
    private static DeliverableResponse Map(Deliverable x) => new(x.Id, x.Name, x.ProjectId, x.BudgetHours, x.Details?.InitialEstimateHours ?? 0, x.Details?.ActualHours ?? 0);
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public async Task<HierarchyResponse> GetAsync(CancellationToken ct)
    {
        var positions = await db.HierarchyPositions.AsNoTracking()
            .ToDictionaryAsync(x => (x.ItemKind, x.ItemId), x => x.SortOrder, ct);
        var customers = await db.Customers.AsNoTracking().Include(x => x.Details).ToListAsync(ct);
        var projects = await db.Projects.AsNoTracking().Include(x => x.Details).ToListAsync(ct);
        var deliverables = await db.Deliverables.AsNoTracking().Include(x => x.Details).ToListAsync(ct);
        return new(
            customers.OrderBy(x => Position(positions, "customers", x.Id)).ThenBy(x => x.Name).ThenBy(x => x.Id).Select(Map).ToList(),
            projects.OrderBy(x => Position(positions, "projects", x.Id)).ThenBy(x => x.Name).ThenBy(x => x.Id).Select(Map).ToList(),
            deliverables.OrderBy(x => Position(positions, "deliverables", x.Id)).ThenBy(x => x.Name).ThenBy(x => x.Id).Select(Map).ToList());
    }

    public async Task<CustomerResponse?> SaveCustomerAsync(string? id, CustomerRequest request, CancellationToken ct)
    {
        var entity = id is null ? new Customer() : await db.Customers.Include(x => x.Details).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return null;
        entity.Name = request.Name.Trim();
        entity.Details ??= new CustomerDetails { CustomerId = entity.Id };
        entity.Details.DebitorNo = Clean(request.DebitorNo);
        entity.Details.CrmUrl = Clean(request.CrmUrl);
        if (id is null)
        {
            db.Customers.Add(entity);
            db.HierarchyPositions.Add(new HierarchyPosition
            {
                ItemKind = "customers",
                ItemId = entity.Id,
                ParentId = "",
                SortOrder = await NextPositionAsync("customers", "", ct)
            });
        }
        await db.SaveChangesAsync(ct);
        return Map(entity);
    }

    public async Task<ProjectResponse?> SaveProjectAsync(string? id, ProjectRequest request, CancellationToken ct)
    {
        if (!await db.Customers.AnyAsync(x => x.Id == request.CustomerId, ct)) return null;
        var entity = id is null ? new Project() : await db.Projects.Include(x => x.Details).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return null;
        var previousCustomerId = entity.CustomerId;
        var parentChanged = id is not null && previousCustomerId != request.CustomerId;
        var key = request.ShortKey.ToUpperInvariant();
        if (entity.Details?.ShortKey is { Length: > 0 } existing && existing != key)
            throw new HierarchyConflictException("KEY_LOCKED", "The project short key cannot be changed after it is set.");
        if (await db.Set<ProjectDetails>().AnyAsync(x => x.ShortKey == key && x.ProjectId != entity.Id, ct))
            throw new HierarchyConflictException("KEY_EXISTS", "This project short key is already in use.");
        entity.Name = request.Name.Trim();
        entity.CustomerId = request.CustomerId;
        entity.Details ??= new ProjectDetails { ProjectId = entity.Id };
        entity.Details.ShortKey = key;
        entity.Details.CrmLink = Clean(request.CrmLink);
        entity.Details.CrmUrl = Clean(request.CrmUrl);
        entity.Details.InternalOrderNumber = Clean(request.InternalOrderNumber);
        entity.Details.CustomerOrderNumber = Clean(request.CustomerOrderNumber);
        entity.Details.DefaultAssignee = Clean(request.DefaultAssignee);
        if (id is null)
        {
            db.Projects.Add(entity);
            await projectBoards.EnsureDefaultColumnsAsync(entity.Id, ct);
            db.HierarchyPositions.Add(new HierarchyPosition
            {
                ItemKind = "projects",
                ItemId = entity.Id,
                ParentId = request.CustomerId,
                SortOrder = await NextPositionAsync("projects", request.CustomerId, ct)
            });
        }
        else if (parentChanged)
        {
            var position = await GetOrCreatePositionAsync("projects", entity.Id, previousCustomerId, ct);
            await ReindexPositionsAsync("projects", previousCustomerId, entity.Id, ct);
            position.ParentId = request.CustomerId;
            position.SortOrder = await NextPositionAsync("projects", request.CustomerId, ct);
        }
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException e) when (e.InnerException is SqliteException { SqliteExtendedErrorCode: 2067 })
        { throw new HierarchyConflictException("KEY_EXISTS", "This project short key is already in use."); }
        return Map(entity);
    }

    public async Task<DeliverableResponse?> SaveDeliverableAsync(string? id, DeliverableRequest request, CancellationToken ct)
    {
        var project = await db.Projects.Include(x => x.Details).FirstOrDefaultAsync(x => x.Id == request.ProjectId, ct);
        if (project is null) return null;
        var entity = id is null ? new Deliverable() : await db.Deliverables.Include(x => x.Details).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return null;
        var previousProjectId = entity.ProjectId;
        var parentChanged = id is not null && previousProjectId != request.ProjectId;
        if (id is null)
        {
            if (string.IsNullOrEmpty(project.Details?.ShortKey))
                throw new HierarchyConflictException("KEY_REQUIRED", "Set a project short key before creating a deliverable.");
            entity.Id = $"{project.Details.ShortKey}-D-{Guid.NewGuid():N}";
        }
        entity.Name = request.Name.Trim();
        entity.ProjectId = request.ProjectId;
        entity.BudgetHours = request.BudgetHours;
        entity.Details ??= new DeliverableDetails { DeliverableId = entity.Id };
        entity.Details.InitialEstimateHours = request.InitialEstimateHours;
        entity.Details.ActualHours = request.ActualHours;
        if (id is null)
        {
            db.Deliverables.Add(entity);
            db.HierarchyPositions.Add(new HierarchyPosition
            {
                ItemKind = "deliverables",
                ItemId = entity.Id,
                ParentId = request.ProjectId,
                SortOrder = await NextPositionAsync("deliverables", request.ProjectId, ct)
            });
        }
        else if (parentChanged)
        {
            var position = await GetOrCreatePositionAsync("deliverables", entity.Id, previousProjectId, ct);
            await ReindexPositionsAsync("deliverables", previousProjectId, entity.Id, ct);
            position.ParentId = request.ProjectId;
            position.SortOrder = await NextPositionAsync("deliverables", request.ProjectId, ct);
        }
        await db.SaveChangesAsync(ct);
        return Map(entity);
    }

    public async Task<bool> MoveAsync(string kind, string id, string? parentId, int index, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var moved = kind switch
        {
            "customers" when parentId is null => await MoveCustomerAsync(id, index, ct),
            "projects" when !string.IsNullOrWhiteSpace(parentId) => await MoveProjectAsync(id, parentId, index, ct),
            "deliverables" when !string.IsNullOrWhiteSpace(parentId) => await MoveDeliverableAsync(id, parentId, index, ct),
            "tasks" when !string.IsNullOrWhiteSpace(parentId) => await MoveTaskAsync(id, parentId, index, ct),
            _ => false
        };
        if (!moved) return false;
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return true;
    }

    private async Task<bool> MoveCustomerAsync(string id, int index, CancellationToken ct)
    {
        if (!await db.Customers.AnyAsync(x => x.Id == id, ct)) return false;
        var position = await GetOrCreatePositionAsync("customers", id, "", ct);
        var siblings = await GetPositionsAsync("customers", "", ct);
        InsertAt(siblings, position, index);
        return true;
    }

    private async Task<bool> MoveProjectAsync(string id, string customerId, int index, CancellationToken ct)
    {
        if (!await db.Customers.AnyAsync(x => x.Id == customerId, ct)) return false;
        var item = await db.Projects.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (item is null) return false;
        var position = await GetOrCreatePositionAsync("projects", id, item.CustomerId, ct);
        var source = await GetPositionsAsync("projects", item.CustomerId, ct);
        var target = item.CustomerId == customerId ? source : await GetPositionsAsync("projects", customerId, ct);
        if (item.CustomerId != customerId)
        {
            source.Remove(position);
            Reindex(source);
            item.CustomerId = customerId;
            position.ParentId = customerId;
        }
        InsertAt(target, position, index);
        return true;
    }

    private async Task<bool> MoveDeliverableAsync(string id, string projectId, int index, CancellationToken ct)
    {
        if (!await db.Projects.AnyAsync(x => x.Id == projectId, ct)) return false;
        var item = await db.Deliverables.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (item is null) return false;
        var position = await GetOrCreatePositionAsync("deliverables", id, item.ProjectId, ct);
        var source = await GetPositionsAsync("deliverables", item.ProjectId, ct);
        var target = item.ProjectId == projectId ? source : await GetPositionsAsync("deliverables", projectId, ct);
        if (item.ProjectId != projectId)
        {
            source.Remove(position);
            Reindex(source);
            item.ProjectId = projectId;
            position.ParentId = projectId;
            var tasks = await db.Tasks
                .Include(task => task.ProjectBoardPosition)
                .Where(task => task.DeliverableAssignment != null &&
                    task.DeliverableAssignment.DeliverableId == item.Id)
                .ToListAsync(ct);
            foreach (var task in tasks)
                await projectBoards.EnsureTaskPositionAsync(task, projectId, task.ColumnName, ct);
        }
        InsertAt(target, position, index);
        return true;
    }

    private async Task<bool> MoveTaskAsync(string id, string deliverableId, int index, CancellationToken ct)
    {
        if (!await db.Deliverables.AnyAsync(x => x.Id == deliverableId, ct)) return false;
        var task = await db.Tasks
            .Include(x => x.DeliverableAssignment)
            .ThenInclude(assignment => assignment!.Deliverable)
            .Include(x => x.ProjectBoardPosition)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        if (task is null) return false;
        var assignment = task.DeliverableAssignment;
        var sourceDeliverableId = assignment?.DeliverableId;
        var sourceProjectId = assignment?.Deliverable?.ProjectId;
        var targetProjectId = await db.Deliverables
            .Where(x => x.Id == deliverableId)
            .Select(x => x.ProjectId)
            .FirstAsync(ct);
        var position = await GetOrCreatePositionAsync("tasks", id, sourceDeliverableId ?? deliverableId, ct);
        var source = sourceDeliverableId is null
            ? new List<HierarchyPosition>()
            : await GetPositionsAsync("tasks", sourceDeliverableId, ct);
        var target = sourceDeliverableId == deliverableId
            ? source
            : await GetPositionsAsync("tasks", deliverableId, ct);
        if (assignment is null)
        {
            assignment = new TaskDeliverable { TaskId = task.Id, DeliverableId = deliverableId };
            db.TaskDeliverables.Add(assignment);
            position.ParentId = deliverableId;
        }
        else if (sourceDeliverableId != deliverableId)
        {
            source.Remove(position);
            Reindex(source);
            assignment.DeliverableId = deliverableId;
            position.ParentId = deliverableId;
        }
        if (sourceProjectId != targetProjectId)
            await projectBoards.EnsureTaskPositionAsync(task, targetProjectId, task.ColumnName, ct);
        InsertAt(target, position, index);
        return true;
    }

    private async Task<int> NextPositionAsync(string kind, string parentId, CancellationToken ct)
    {
        return (await db.HierarchyPositions.Where(x => x.ItemKind == kind && x.ParentId == parentId)
            .Select(x => (int?)x.SortOrder).MaxAsync(ct) ?? -1) + 1;
    }

    private async Task<HierarchyPosition> GetOrCreatePositionAsync(string kind, string id, string parentId, CancellationToken ct)
    {
        var position = await db.HierarchyPositions.FirstOrDefaultAsync(x => x.ItemKind == kind && x.ItemId == id, ct);
        if (position is not null) return position;
        position = new HierarchyPosition
        {
            ItemKind = kind,
            ItemId = id,
            ParentId = parentId,
            SortOrder = await NextPositionAsync(kind, parentId, ct)
        };
        db.HierarchyPositions.Add(position);
        return position;
    }

    private Task<List<HierarchyPosition>> GetPositionsAsync(string kind, string parentId, CancellationToken ct)
    {
        return db.HierarchyPositions.Where(x => x.ItemKind == kind && x.ParentId == parentId)
            .OrderBy(x => x.SortOrder).ThenBy(x => x.ItemId).ToListAsync(ct);
    }

    private async System.Threading.Tasks.Task ReindexPositionsAsync(string kind, string parentId, string excludeId, CancellationToken ct)
    {
        var siblings = await GetPositionsAsync(kind, parentId, ct);
        siblings.RemoveAll(x => x.ItemId == excludeId);
        Reindex(siblings);
    }

    private static int Position(IReadOnlyDictionary<(string ItemKind, string ItemId), int> positions, string kind, string id)
    {
        return positions.TryGetValue((kind, id), out var order) ? order : int.MaxValue;
    }

    private static void InsertAt(List<HierarchyPosition> siblings, HierarchyPosition item, int index)
    {
        siblings.Remove(item);
        siblings.Insert(Math.Clamp(index, 0, siblings.Count), item);
        Reindex(siblings);
    }

    private static void Reindex(IEnumerable<HierarchyPosition> siblings)
    {
        var index = 0;
        foreach (var sibling in siblings)
            sibling.SortOrder = index++;
    }
}
