using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrKanban.Services;

namespace PrKanban.Controllers;

/// <summary>Shared project hierarchy and administrator maintenance.</summary>
[ApiController]
[Route("api/v1/hierarchy")]
[Authorize(Roles = "Admin,User")]
public sealed class ProjectHierarchyController(IProjectHierarchyService hierarchy) : ControllerBase
{
    private IActionResult ConflictResult(HierarchyConflictException error)
    {
        var problem = new ProblemDetails { Status = 409, Title = "Hierarchy conflict", Detail = error.Message };
        problem.Extensions["code"] = error.Code;
        return Conflict(problem);
    }

    /// <summary>Returns customers, projects and deliverables.</summary>
    [HttpGet]
    public async Task<ActionResult<HierarchyResponse>> Get(CancellationToken ct) => Ok(await hierarchy.GetAsync(ct));

    /// <summary>Creates a customer.</summary>
    [HttpPost("customers")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateCustomer(CustomerRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name)) return Problem(statusCode: 400, detail: "Name is required.");
        var result = await hierarchy.SaveCustomerAsync(null, request, ct);
        return CreatedAtAction(nameof(Get), result);
    }

    /// <summary>Updates a customer.</summary>
    [HttpPut("customers/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateCustomer(string id, CustomerRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name)) return Problem(statusCode: 400, detail: "Name is required.");
        var result = await hierarchy.SaveCustomerAsync(id, request, ct);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>Creates a project under an existing customer.</summary>
    [HttpPost("projects")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateProject(ProjectRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name)) return Problem(statusCode: 400, detail: "Name is required.");
        try
        {
            var result = await hierarchy.SaveProjectAsync(null, request, ct);
            return result is null ? Problem(statusCode: 400, detail: "Customer not found.") : CreatedAtAction(nameof(Get), result);
        }
        catch (HierarchyConflictException e) { return ConflictResult(e); }
    }

    /// <summary>Updates a project and its customer.</summary>
    [HttpPut("projects/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateProject(string id, ProjectRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name)) return Problem(statusCode: 400, detail: "Name is required.");
        try
        {
            var result = await hierarchy.SaveProjectAsync(id, request, ct);
            return result is null ? NotFound() : Ok(result);
        }
        catch (HierarchyConflictException e) { return ConflictResult(e); }
    }

    /// <summary>Creates a deliverable and its time budget.</summary>
    [HttpPost("deliverables")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateDeliverable(DeliverableRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name)) return Problem(statusCode: 400, detail: "Name is required.");
        try
        {
            var result = await hierarchy.SaveDeliverableAsync(null, request, ct);
            return result is null ? Problem(statusCode: 400, detail: "Project not found.") : CreatedAtAction(nameof(Get), result);
        }
        catch (HierarchyConflictException e) { return ConflictResult(e); }
    }

    /// <summary>Updates a deliverable and its time budget.</summary>
    [HttpPut("deliverables/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateDeliverable(string id, DeliverableRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name)) return Problem(statusCode: 400, detail: "Name is required.");
        try
        {
            var result = await hierarchy.SaveDeliverableAsync(id, request, ct);
            return result is null ? NotFound() : Ok(result);
        }
        catch (HierarchyConflictException e) { return ConflictResult(e); }
    }

    /// <summary>Moves and reorders an item in the hierarchy. Admins and users share this permission.</summary>
    [HttpPut("{kind}/{id}/position")]
    public async Task<IActionResult> Move(string kind, string id, MoveHierarchyItemRequest request, CancellationToken ct)
    {
        if (kind is not ("customers" or "projects" or "deliverables" or "tasks"))
            return BadRequest(new ProblemDetails { Status = 400, Title = "Invalid hierarchy item type." });
        if ((kind == "customers" && request.ParentId is not null) ||
            (kind != "customers" && string.IsNullOrWhiteSpace(request.ParentId)))
            return BadRequest(new ProblemDetails { Status = 400, Title = "Invalid hierarchy parent." });

        var moved = await hierarchy.MoveAsync(kind, id, request.ParentId, request.Index, ct);
        return moved ? Ok(new { success = true }) : NotFound();
    }
}
