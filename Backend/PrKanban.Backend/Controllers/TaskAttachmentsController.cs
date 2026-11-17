using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrKanban.Services;

namespace PrKanban.Controllers;

/// <summary>Uploads, lists, downloads, and removes files attached to tasks.</summary>
[ApiController]
[Route("api/v1/tasks/{taskId}/attachments")]
[Authorize(Roles = "Admin,User")]
public sealed class TaskAttachmentsController(ITaskAttachmentService attachments) : ControllerBase
{
    private const long MaximumRequestSize = TaskAttachmentService.MaximumFileSize + 128 * 1024;

    /// <summary>Lists attachments for a task visible to the current account.</summary>
    [HttpGet]
    public async Task<IActionResult> GetForTask(string taskId, CancellationToken cancellationToken)
    {
        var result = await attachments.GetForTaskAsync(taskId, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>Returns metadata for one attachment.</summary>
    [HttpGet("{attachmentId}")]
    public async Task<IActionResult> Get(string taskId, string attachmentId, CancellationToken cancellationToken)
    {
        var attachment = await attachments.GetAsync(taskId, attachmentId, cancellationToken);
        return attachment is null ? NotFound() : Ok(attachment);
    }

    /// <summary>Uploads a file up to 20 MB to the task's SQLite attachment store.</summary>
    [HttpPost]
    [RequestSizeLimit(MaximumRequestSize)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaximumRequestSize)]
    public async Task<IActionResult> Upload(
        string taskId,
        [FromForm] IFormFile? file,
        [FromForm] bool isInlineImage,
        CancellationToken cancellationToken)
    {
        if (file is null)
            return BadRequest(new ProblemDetails { Status = 400, Title = "A file is required." });

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        await using var content = file.OpenReadStream();
        var result = await attachments.SaveAsync(
            taskId, userId, file.FileName, file.Length, content, isInlineImage, cancellationToken);
        if (result.Attachment is not null)
            return CreatedAtAction(nameof(Get),
                new { taskId, attachmentId = result.Attachment.Id },
                result.Attachment);

        return result.StatusCode switch
        {
            StatusCodes.Status404NotFound => NotFound(),
            StatusCodes.Status413PayloadTooLarge => StatusCode(result.StatusCode,
                new ProblemDetails { Status = result.StatusCode, Title = result.Error }),
            _ => BadRequest(new ProblemDetails { Status = result.StatusCode, Title = result.Error })
        };
    }

    /// <summary>Downloads an attachment or serves a validated raster image inline.</summary>
    [HttpGet("{attachmentId}/content")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> GetContent(
        string taskId, string attachmentId, CancellationToken cancellationToken)
    {
        var file = await attachments.GetFileAsync(taskId, attachmentId, cancellationToken);
        if (file is null)
            return NotFound();

        Response.Headers["X-Content-Type-Options"] = "nosniff";
        return file.IsInlineImage && file.InlineContentType is not null
            ? File(file.Content, file.InlineContentType)
            : File(file.Content, "application/octet-stream", file.FileName);
    }

    /// <summary>Deletes an attachment from a shared task.</summary>
    [HttpDelete("{attachmentId}")]
    public async Task<IActionResult> Delete(
        string taskId, string attachmentId, CancellationToken cancellationToken)
    {
        if (!await attachments.DeleteAsync(taskId, attachmentId, cancellationToken))
            return NotFound();
        return NoContent();
    }
}
