using System.ComponentModel.DataAnnotations;

namespace PrKanban.Services;

/// <summary>Customer, project and deliverable structure.</summary>
public sealed record HierarchyResponse(IReadOnlyList<CustomerResponse> Customers, IReadOnlyList<ProjectResponse> Projects, IReadOnlyList<DeliverableResponse> Deliverables);
/// <summary>A customer.</summary>
public sealed record CustomerResponse(string Id, string Name, string? DebitorNo, string? CrmUrl);
/// <summary>A project belonging to a customer.</summary>
public sealed record ProjectResponse(string Id, string Name, string CustomerId, string? ShortKey, string? CrmLink, string? CrmUrl, string? InternalOrderNumber, string? CustomerOrderNumber, string? DefaultAssignee);
/// <summary>A deliverable and its budget in hours.</summary>
public sealed record DeliverableResponse(string Id, string Name, string ProjectId, double BudgetHours, double InitialEstimateHours, double ActualHours);
/// <summary>A customer name.</summary>
public sealed record CustomerRequest
{
    [Required, MaxLength(200)] public required string Name { get; init; }
    [MaxLength(100)] public string? DebitorNo { get; init; }
    [Url, MaxLength(2000), RegularExpression(@"^https?://.*")] public string? CrmUrl { get; init; }
}
/// <summary>A project name and its customer.</summary>
public sealed record ProjectRequest
{
    [Required, MaxLength(200)] public required string Name { get; init; }
    [Required] public required string CustomerId { get; init; }
    [Required, RegularExpression(@"^[A-Za-z][A-Za-z0-9]{1,11}$")] public required string ShortKey { get; init; }
    [Url, MaxLength(2000), RegularExpression(@"^https?://.*")] public string? CrmLink { get; init; }
    [Url, MaxLength(2000), RegularExpression(@"^https?://.*")] public string? CrmUrl { get; init; }
    [MaxLength(100)] public string? InternalOrderNumber { get; init; }
    [MaxLength(100)] public string? CustomerOrderNumber { get; init; }
    [MaxLength(256)] public string? DefaultAssignee { get; init; }
}
/// <summary>A deliverable, its project and budget in hours.</summary>
public sealed record DeliverableRequest
{
    [Required, MaxLength(200)] public required string Name { get; init; }
    [Required] public required string ProjectId { get; init; }
    [Range(0, 1000000000)] public required double BudgetHours { get; init; }
    [Range(0, 1000000000)] public double InitialEstimateHours { get; init; }
    [Range(0, 1000000000)] public double ActualHours { get; init; }
}

/// <summary>Moves an item to a new parent and sibling position.</summary>
public sealed record MoveHierarchyItemRequest
{
    [MaxLength(100)] public string? ParentId { get; init; }
    [Range(0, int.MaxValue)] public required int Index { get; init; }
}

public sealed class HierarchyConflictException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}

public interface IProjectHierarchyService
{
    Task<HierarchyResponse> GetAsync(CancellationToken ct);
    Task<CustomerResponse?> SaveCustomerAsync(string? id, CustomerRequest request, CancellationToken ct);
    Task<ProjectResponse?> SaveProjectAsync(string? id, ProjectRequest request, CancellationToken ct);
    Task<DeliverableResponse?> SaveDeliverableAsync(string? id, DeliverableRequest request, CancellationToken ct);
    Task<bool> MoveAsync(string kind, string id, string? parentId, int index, CancellationToken ct);
}
