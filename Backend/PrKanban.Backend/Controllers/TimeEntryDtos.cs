namespace PrKanban.Controllers;

/// <summary>
/// Payload for creating a new time entry
/// </summary>
public sealed record CreateTimeEntryRequest
{
    /// <summary>
    /// Start date and time (ISO 8601 format, UTC)
    /// </summary>
    public required DateTimeOffset StartTime { get; init; }

    /// <summary>
    /// End date and time (ISO 8601 format, UTC), optional if DurationHours is provided
    /// </summary>
    public DateTimeOffset? EndTime { get; init; }

    /// <summary>
    /// Duration in hours (optional if StartTime and EndTime are provided)
    /// </summary>
    public double? DurationHours { get; init; }

    /// <summary>
    /// Optional comment/description
    /// </summary>
    public string? Comment { get; init; }

    /// <summary>
    /// Whether this time is billable (default: true)
    /// </summary>
    public bool IsBillable { get; init; } = true;
}

/// <summary>
/// Payload for updating an existing time entry
/// </summary>
public sealed record UpdateTimeEntryRequest
{
    /// <summary>
    /// Start date and time (ISO 8601 format, UTC)
    /// </summary>
    public DateTimeOffset? StartTime { get; init; }

    /// <summary>
    /// End date and time (ISO 8601 format, UTC)
    /// </summary>
    public DateTimeOffset? EndTime { get; init; }

    /// <summary>
    /// Duration in hours
    /// </summary>
    public double? DurationHours { get; init; }

    /// <summary>
    /// Optional comment/description
    /// </summary>
    public string? Comment { get; init; }

    /// <summary>
    /// Whether this time is billable
    /// </summary>
    public bool? IsBillable { get; init; }
}

/// <summary>
/// Represents a time entry returned by the API
/// </summary>
public sealed record TimeEntryResponse(
    string Id,
    string TaskId,
    string UserId,
    string? UserEmail,
    DateTimeOffset StartTime,
    DateTimeOffset? EndTime,
    double DurationHours,
    string? Comment,
    bool IsBillable,
    bool Locked,
    DateTimeOffset? LockedAt,
    string? LockedPeriod,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>
/// List of time entries
/// </summary>
public sealed record TimeEntryListResponse(
    IReadOnlyList<TimeEntryResponse> Entries);

/// <summary>
/// Payload for locking or unlocking a single time entry
/// </summary>
public sealed record SetTimeEntryLockRequest
{
    /// <summary>Desired lock state</summary>
    public required bool Locked { get; init; }
}

/// <summary>
/// Payload for locking all time entries of a billing period
/// </summary>
public sealed record LockPeriodRequest
{
    /// <summary>Tasks whose entries should be locked</summary>
    public required string[] TaskIds { get; init; }

    /// <summary>Period start (inclusive, UTC)</summary>
    public required DateTimeOffset From { get; init; }

    /// <summary>Period end (inclusive, UTC)</summary>
    public required DateTimeOffset To { get; init; }

    /// <summary>Optional user filter (recorder user id)</summary>
    public string? UserId { get; init; }

    /// <summary>Human-readable period label stored on the locked entries</summary>
    public string? LockedPeriod { get; init; }
}

/// <summary>
/// Result of a period lock operation
/// </summary>
public sealed record LockPeriodResponse(int LockedCount);
