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
    DateTimeOffset StartTime,
    DateTimeOffset? EndTime,
    double DurationHours,
    string? Comment,
    bool IsBillable,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>
/// List of time entries
/// </summary>
public sealed record TimeEntryListResponse(
    IReadOnlyList<TimeEntryResponse> Entries);
