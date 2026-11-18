using System.ComponentModel.DataAnnotations.Schema;

namespace PrKanban.Models;

public sealed class Customer
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "";
    public CustomerDetails? Details { get; set; }
}

public sealed class Project
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "";
    public string CustomerId { get; set; } = "";
    public Customer Customer { get; set; } = null!;
    public ProjectDetails? Details { get; set; }
}

public sealed class Deliverable
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "";
    public string ProjectId { get; set; } = "";
    public Project Project { get; set; } = null!;
    public double BudgetHours { get; set; }
    public DeliverableDetails? Details { get; set; }
}

public sealed class CustomerDetails
{
    public string CustomerId { get; set; } = "";
    public string? DebitorNo { get; set; }
    public string? CrmUrl { get; set; }
}

public sealed class ProjectDetails
{
    public string ProjectId { get; set; } = "";
    public string? ShortKey { get; set; }
    public string? CrmLink { get; set; }
    public string? CrmUrl { get; set; }
    public string? InternalOrderNumber { get; set; }
    public string? CustomerOrderNumber { get; set; }
    public string? DefaultAssignee { get; set; }
}

public sealed class DeliverableDetails
{
    public string DeliverableId { get; set; } = "";
    public double InitialEstimateHours { get; set; }
    public double ActualHours { get; set; }
}

public sealed class TaskDeliverable
{
    public string TaskId { get; set; } = "";
    public string DeliverableId { get; set; } = "";
    [NotMapped]
    public int SortOrder { get; set; }
    public Deliverable Deliverable { get; set; } = null!;
}

public sealed class HierarchyPosition
{
    public string ItemKind { get; set; } = "";
    public string ItemId { get; set; } = "";
    public string ParentId { get; set; } = "";
    public int SortOrder { get; set; }
}
