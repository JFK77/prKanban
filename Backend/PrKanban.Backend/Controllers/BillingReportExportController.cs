using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrKanban.Services;

namespace PrKanban.Controllers;

/// <summary>Exports billing reports as Excel workbooks.</summary>
[ApiController]
[Route("api/v1/billing-report")]
[Authorize(Roles = "Admin,User")]
public sealed class BillingReportExportController(ITableExportService exports) : ControllerBase
{
    /// <summary>Creates a billing workbook from metadata, summary figures and filtered time entry rows.</summary>
    [HttpPost("export")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public IActionResult Export([FromBody] BillingReportExportRequest request, CancellationToken cancellationToken)
    {
        if (request.Headers.Length is 0 or > 64 || request.Rows.Length > 100000 ||
            request.Metadata.Length > 64 || request.Summary.Length > 64 ||
            request.Headers.Any(value => value is null || value.Length > 32767) ||
            request.Metadata.Concat(request.Summary).Any(pair => pair is null || pair.Any(value => value is null || value.Length > 32767)) ||
            request.Rows.Any(row => row is null || row.Length != request.Headers.Length ||
                row.Any(value => value is null || value.Length > 32767)))
            return BadRequest(new ProblemDetails { Status = 400, Title = "Invalid billing report data." });

        var file = exports.CreateBillingWorkbook(
            request.Title ?? "Abrechnungsreport", request.Metadata, request.Summary,
            request.Headers, request.Rows, cancellationToken);
        return File(file, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "billing-report.xlsx");
    }
}

/// <summary>Billing report content assembled by the Billing Report Assistant.</summary>
public sealed record BillingReportExportRequest
{
    /// <summary>Report title shown in the first sheet row.</summary>
    public string? Title { get; init; }

    /// <summary>Label/value metadata pairs (customer, project, period, creator, …).</summary>
    [Required]
    public required string[][] Metadata { get; init; }

    /// <summary>Label/value summary pairs (budget, actual, billable, …).</summary>
    [Required]
    public required string[][] Summary { get; init; }

    /// <summary>Entry table column headings in display order.</summary>
    [Required]
    public required string[] Headers { get; init; }

    /// <summary>Entry table rows in display order.</summary>
    [Required]
    public required string[][] Rows { get; init; }
}
