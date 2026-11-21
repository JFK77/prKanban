using PrKanban.Controllers;

namespace PrKanban.Services;

/// <summary>
/// Interface for time entry management
/// </summary>
public interface ITimeEntryService
{
    /// <summary>
    /// Get a time entry by ID
    /// </summary>
    Task<TimeEntryResponse?> GetByIdAsync(string taskId, string timeEntryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all time entries for a task
    /// </summary>
    Task<IReadOnlyList<TimeEntryResponse>> GetByTaskIdAsync(string taskId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new time entry
    /// </summary>
    Task<TimeEntryResponse> CreateAsync(string taskId, string userId, CreateTimeEntryRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing time entry
    /// </summary>
    Task<TimeEntryResponse> UpdateAsync(string taskId, string timeEntryId, UpdateTimeEntryRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a time entry
    /// </summary>
    Task DeleteAsync(string taskId, string timeEntryId, CancellationToken cancellationToken = default);
}
