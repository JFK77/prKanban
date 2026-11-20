using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrKanban.Services;

namespace PrKanban.Controllers;

/// <summary>Exports the current task-table contents as an Excel workbook.</summary>
[ApiController]
[Route("api/v1/task-table")]
[Authorize(Roles = "Admin,User")]
public sealed class TaskTableExportController(ITableExportService exports) : ControllerBase
{
    /// <summary>Creates a workbook from the displayed headers and filtered task rows.</summary>
    [HttpPost("export")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public IActionResult Export([FromBody] ExportTaskTableRequest request, CancellationToken cancellationToken)
    {
        if (request.Headers.Length is 0 or > 16384 || request.Rows.Length > 1048575 ||
            request.Headers.Any(value => value is null || value.Length > 32767) ||
            request.Rows.Any(row => row is null || row.Length != request.Headers.Length ||
                row.Any(value => value is null || value.Length > 32767)))
            return BadRequest(new ProblemDetails { Status = 400, Title = "Invalid Excel table data." });

        var file = exports.CreateWorkbook(request.Headers, request.Rows, cancellationToken);
        return File(file, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "tasks.xlsx");
    }
}

/// <summary>The visible table columns and filtered task rows to export.</summary>
public sealed record ExportTaskTableRequest
{
    /// <summary>Column headings in display order.</summary>
    [Required]
    public required string[] Headers { get; init; }

    /// <summary>Task rows in display order, including rows in collapsed groups.</summary>
    [Required]
    public required string[][] Rows { get; init; }
}
