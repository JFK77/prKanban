using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PrKanban.Data;
using PrKanban.Models;

namespace PrKanban.Services;

public sealed class ProjectHierarchyService(PrKanbanContext db) : IProjectHierarchyService
{
    private static CustomerResponse Map(Customer x) => new(x.Id, x.Name, x.Details?.DebitorNo, x.Details?.CrmUrl);
    private static ProjectResponse Map(Project x) => new(x.Id, x.Name, x.CustomerId, x.Details?.ShortKey, x.Details?.CrmLink, x.Details?.CrmUrl, x.Details?.InternalOrderNumber, x.Details?.CustomerOrderNumber, x.Details?.DefaultAssignee);
    private static DeliverableResponse Map(Deliverable x) => new(x.Id, x.Name, x.ProjectId, x.BudgetHours, x.Details?.InitialEstimateHours ?? 0, x.Details?.ActualHours ?? 0);
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public async Task<HierarchyResponse> GetAsync(CancellationToken ct)
    {
        var customers = await db.Customers.AsNoTracking().Include(x => x.Details).OrderBy(x => x.Name).ToListAsync(ct);
        var projects = await db.Projects.AsNoTracking().Include(x => x.Details).OrderBy(x => x.Name).ToListAsync(ct);
        var deliverables = await db.Deliverables.AsNoTracking().Include(x => x.Details).OrderBy(x => x.Name).ToListAsync(ct);
        return new(customers.Select(Map).ToList(), projects.Select(Map).ToList(), deliverables.Select(Map).ToList());
    }

    public async Task<CustomerResponse?> SaveCustomerAsync(string? id, CustomerRequest request, CancellationToken ct)
    {
        var entity = id is null ? new Customer() : await db.Customers.Include(x => x.Details).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return null;
        entity.Name = request.Name.Trim();
        entity.Details ??= new CustomerDetails { CustomerId = entity.Id };
        entity.Details.DebitorNo = Clean(request.DebitorNo);
        entity.Details.CrmUrl = Clean(request.CrmUrl);
        if (id is null) db.Customers.Add(entity);
        await db.SaveChangesAsync(ct);
        return Map(entity);
    }

    public async Task<ProjectResponse?> SaveProjectAsync(string? id, ProjectRequest request, CancellationToken ct)
    {
        if (!await db.Customers.AnyAsync(x => x.Id == request.CustomerId, ct)) return null;
        var entity = id is null ? new Project() : await db.Projects.Include(x => x.Details).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return null;
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
        if (id is null) db.Projects.Add(entity);
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
        if (id is null) db.Deliverables.Add(entity);
        await db.SaveChangesAsync(ct);
        return Map(entity);
    }
}
