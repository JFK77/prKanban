using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrKanban.Services;

namespace PrKanban.Controllers;

/// <summary>Project-specific Kanban columns and their master-board mappings.</summary>
[ApiController]
[Route("api/v1/project-boards")]
[Authorize(Roles = "Admin,User")]
public sealed class ProjectBoardsController(IProjectBoardService boards) : ControllerBase
{
    /// <summary>Gets a project board, its columns, mappings and assigned project leads.</summary>
    [HttpGet("{projectId}")]
    public async Task<IActionResult> Get(string projectId, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();
        var board = await boards.GetAsync(projectId, userId, User.IsInRole("Admin"), ct);
        return board is null ? NotFound() : Ok(board);
    }

    /// <summary>Gets a project board column.</summary>
    [HttpGet("{projectId}/columns/{columnId}")]
    public async Task<IActionResult> GetColumn(string projectId, string columnId, CancellationToken ct)
    {
        var result = await boards.GetColumnAsync(projectId, columnId, ct);
        return result.Code == ProjectBoardResultCode.Success ? Ok(result.Value) : NotFound();
    }

    /// <summary>Adds a project-specific column mapped to a master-board category.</summary>
    [HttpPost("{projectId}/columns")]
    public async Task<IActionResult> CreateColumn(
        string projectId, CreateProjectBoardColumnRequest request, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();
        var result = await boards.CreateColumnAsync(projectId, userId, User.IsInRole("Admin"), request, ct);
        if (result.Code != ProjectBoardResultCode.Success) return Failure(result.Code);
        return CreatedAtAction(nameof(GetColumn),
            new { projectId, columnId = result.Value!.Id }, result.Value);
    }

    /// <summary>Renames or remaps an empty project-board column.</summary>
    [HttpPut("{projectId}/columns/{columnId}")]
    public async Task<IActionResult> UpdateColumn(
        string projectId, string columnId, UpdateProjectBoardColumnRequest request, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();
        var result = await boards.UpdateColumnAsync(
            projectId, columnId, userId, User.IsInRole("Admin"), request, ct);
        return result.Code == ProjectBoardResultCode.Success ? Ok(result.Value) : Failure(result.Code);
    }

    /// <summary>Deletes an empty project-board column without removing the last mapping for a category.</summary>
    [HttpDelete("{projectId}/columns/{columnId}")]
    public async Task<IActionResult> DeleteColumn(
        string projectId, string columnId, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();
        var result = await boards.DeleteColumnAsync(
            projectId, columnId, userId, User.IsInRole("Admin"), ct);
        return result.Code == ProjectBoardResultCode.Success ? NoContent() : Failure(result.Code);
    }

    /// <summary>Sets the visual order of all columns in a project board.</summary>
    [HttpPut("{projectId}/columns/order")]
    public async Task<IActionResult> ReorderColumns(
        string projectId, ReorderProjectBoardColumnsRequest request, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();
        var result = await boards.ReorderColumnsAsync(
            projectId, userId, User.IsInRole("Admin"), request, ct);
        return result.Code == ProjectBoardResultCode.Success ? NoContent() : Failure(result.Code);
    }

    /// <summary>Assigns project-board managers. Only administrators may change this list.</summary>
    [HttpPut("{projectId}/leads")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SetLeads(
        string projectId, SetProjectBoardLeadsRequest request, CancellationToken ct)
    {
        var result = await boards.SetLeadsAsync(projectId, request, ct);
        return result.Code == ProjectBoardResultCode.Success ? Ok(result.Value) : Failure(result.Code);
    }

    /// <summary>Moves a task within the project board and updates its master-board category when needed.</summary>
    [HttpPut("{projectId}/tasks/{taskId}/position")]
    public async Task<IActionResult> MoveTask(
        string projectId, string taskId, MoveProjectBoardTaskRequest request, CancellationToken ct)
    {
        var result = await boards.MoveTaskAsync(projectId, taskId, request, ct);
        return result.Code == ProjectBoardResultCode.Success ? NoContent() : Failure(result.Code);
    }

    private IActionResult Failure(ProjectBoardResultCode code) => code switch
    {
        ProjectBoardResultCode.NotFound => NotFound(new ProblemDetails { Status = 404, Title = "Project board resource not found." }),
        ProjectBoardResultCode.Forbidden => StatusCode(StatusCodes.Status403Forbidden,
            new ProblemDetails { Status = 403, Title = "Only project leads or administrators can manage board columns." }),
        ProjectBoardResultCode.Conflict => Conflict(new ProblemDetails
        {
            Status = 409,
            Title = "The column cannot be changed while it contains tasks or is the last mapping for its master category."
        }),
        ProjectBoardResultCode.Invalid => BadRequest(new ProblemDetails { Status = 400, Title = "Invalid project board request." }),
        _ => throw new InvalidOperationException($"Unexpected project board result: {code}.")
    };
}
