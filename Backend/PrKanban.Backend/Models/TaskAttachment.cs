using System.Text.Json.Serialization;

namespace PrKanban.Models;

public sealed class TaskAttachment
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string TaskId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public bool IsInlineImage { get; set; }
    public string? InlineContentType { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedByUserId { get; set; } = string.Empty;

    [JsonIgnore]
    public byte[] Content { get; set; } = Array.Empty<byte>();
}
