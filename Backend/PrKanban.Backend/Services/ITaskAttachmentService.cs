using PrKanban.Models;

namespace PrKanban.Services;

/// <summary>Attachment metadata returned to authorized task viewers.</summary>
public sealed record TaskAttachmentResponse(
    string Id,
    string TaskId,
    string FileName,
    long SizeBytes,
    bool IsInlineImage,
    DateTimeOffset CreatedAt);

/// <summary>Result of saving a task attachment.</summary>
public sealed record TaskAttachmentSaveResult(
    TaskAttachmentResponse? Attachment,
    int StatusCode,
    string? Error);

/// <summary>Attachment content and safe inline-image metadata.</summary>
public sealed record TaskAttachmentFile(
    string FileName,
    byte[] Content,
    bool IsInlineImage,
    string? InlineContentType);

public interface ITaskAttachmentService
{
    Task<IReadOnlyList<TaskAttachmentResponse>?> GetForTaskAsync(
        string taskId, CancellationToken cancellationToken);

    Task<TaskAttachmentResponse?> GetAsync(
        string taskId, string attachmentId, CancellationToken cancellationToken);

    Task<TaskAttachmentSaveResult> SaveAsync(
        string taskId,
        string userId,
        string fileName,
        long length,
        Stream content,
        bool isInlineImage,
        CancellationToken cancellationToken);

    Task<TaskAttachmentFile?> GetFileAsync(
        string taskId, string attachmentId, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(
        string taskId, string attachmentId, CancellationToken cancellationToken);
}
