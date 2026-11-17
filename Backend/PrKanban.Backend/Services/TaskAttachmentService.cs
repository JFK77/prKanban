using Microsoft.EntityFrameworkCore;
using PrKanban.Data;
using PrKanban.Models;

namespace PrKanban.Services;

public sealed class TaskAttachmentService(PrKanbanContext database) : ITaskAttachmentService
{
    public const long MaximumFileSize = 20L * 1024 * 1024;

    public async Task<IReadOnlyList<TaskAttachmentResponse>?> GetForTaskAsync(
        string taskId, CancellationToken cancellationToken)
    {
        if (!await database.Tasks.AnyAsync(task => task.Id == taskId, cancellationToken))
            return null;

        var attachments = await database.TaskAttachments.AsNoTracking()
            .Where(attachment => attachment.TaskId == taskId)
            .OrderBy(attachment => attachment.CreatedAt)
            .Select(attachment => new
            {
                attachment.Id,
                attachment.TaskId,
                attachment.FileName,
                attachment.SizeBytes,
                attachment.IsInlineImage,
                attachment.CreatedAt
            })
            .ToListAsync(cancellationToken);
        return attachments.Select(attachment => new TaskAttachmentResponse(
            attachment.Id,
            attachment.TaskId,
            attachment.FileName,
            attachment.SizeBytes,
            attachment.IsInlineImage,
            ToDateTimeOffset(attachment.CreatedAt))).ToList();
    }

    public async Task<TaskAttachmentResponse?> GetAsync(
        string taskId, string attachmentId, CancellationToken cancellationToken)
    {
        var attachment = await database.TaskAttachments.AsNoTracking()
            .Where(attachment => attachment.TaskId == taskId && attachment.Id == attachmentId)
            .Select(item => new
            {
                item.Id,
                item.TaskId,
                item.FileName,
                item.SizeBytes,
                item.IsInlineImage,
                item.CreatedAt
            })
            .SingleOrDefaultAsync(cancellationToken);
        return attachment is null
            ? null
            : new TaskAttachmentResponse(
                attachment.Id,
                attachment.TaskId,
                attachment.FileName,
                attachment.SizeBytes,
                attachment.IsInlineImage,
                ToDateTimeOffset(attachment.CreatedAt));
    }

    public async Task<TaskAttachmentSaveResult> SaveAsync(
        string taskId,
        string userId,
        string fileName,
        long length,
        Stream content,
        bool isInlineImage,
        CancellationToken cancellationToken)
    {
        if (length <= 0)
            return new(null, StatusCodes.Status400BadRequest, "The file is empty.");
        if (length > MaximumFileSize)
            return new(null, StatusCodes.Status413PayloadTooLarge, "Files must be 20 MB or smaller.");

        var taskExists = await database.Tasks.AnyAsync(task => task.Id == taskId, cancellationToken);
        if (!taskExists)
            return new(null, StatusCodes.Status404NotFound, "Task not found.");

        await using var buffer = new MemoryStream((int)length);
        await content.CopyToAsync(buffer, cancellationToken);
        if (buffer.Length != length)
            return new(null, StatusCodes.Status400BadRequest, "The uploaded file length did not match.");

        var bytes = buffer.ToArray();
        var inlineContentType = isInlineImage ? DetectSafeImageType(bytes) : null;
        if (isInlineImage && inlineContentType is null)
            return new(null, StatusCodes.Status400BadRequest, "Only PNG, JPEG, GIF, and WebP images can be pasted inline.");

        var attachment = new TaskAttachment
        {
            TaskId = taskId,
            FileName = NormalizeFileName(fileName),
            SizeBytes = bytes.LongLength,
            IsInlineImage = isInlineImage,
            InlineContentType = inlineContentType,
            CreatedByUserId = userId,
            Content = bytes
        };
        database.TaskAttachments.Add(attachment);
        await database.SaveChangesAsync(cancellationToken);
        return new(ToResponse(attachment), StatusCodes.Status201Created, null);
    }

    public async Task<TaskAttachmentFile?> GetFileAsync(
        string taskId, string attachmentId, CancellationToken cancellationToken)
    {
        var attachment = await database.TaskAttachments.AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.TaskId == taskId && item.Id == attachmentId,
                cancellationToken);
        return attachment is null
            ? null
            : new(
                attachment.FileName,
                attachment.Content,
                attachment.IsInlineImage,
                attachment.InlineContentType);
    }

    public async Task<bool> DeleteAsync(
        string taskId, string attachmentId, CancellationToken cancellationToken)
    {
        var attachment = await database.TaskAttachments
            .Where(item => item.TaskId == taskId && item.Id == attachmentId)
            .SingleOrDefaultAsync(cancellationToken);
        if (attachment is null)
            return false;

        database.TaskAttachments.Remove(attachment);
        await database.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static TaskAttachmentResponse ToResponse(TaskAttachment attachment) =>
        new(attachment.Id, attachment.TaskId, attachment.FileName, attachment.SizeBytes,
            attachment.IsInlineImage,
            ToDateTimeOffset(attachment.CreatedAt));

    private static DateTimeOffset ToDateTimeOffset(DateTime createdAt) =>
        new(DateTime.SpecifyKind(createdAt, DateTimeKind.Utc));

    private static string NormalizeFileName(string fileName)
    {
        var normalized = fileName.Replace('\\', '/');
        normalized = normalized[(normalized.LastIndexOf('/') + 1)..];
        var safe = new string(normalized.Where(character => !char.IsControl(character)).ToArray()).Trim();
        return string.IsNullOrWhiteSpace(safe) ? "attachment.bin" : safe[..Math.Min(safe.Length, 255)];
    }

    private static string? DetectSafeImageType(ReadOnlySpan<byte> content)
    {
        if (content.Length >= 8
            && content[..8].SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }))
            return "image/png";
        if (content.Length >= 3 && content[0] == 0xff && content[1] == 0xd8 && content[2] == 0xff)
            return "image/jpeg";
        if (content.Length >= 6
            && (content[..6].SequenceEqual("GIF87a"u8) || content[..6].SequenceEqual("GIF89a"u8)))
            return "image/gif";
        if (content.Length >= 12
            && content[..4].SequenceEqual("RIFF"u8)
            && content.Slice(8, 4).SequenceEqual("WEBP"u8))
            return "image/webp";
        return null;
    }
}
