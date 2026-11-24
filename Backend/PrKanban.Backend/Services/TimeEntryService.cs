using Microsoft.EntityFrameworkCore;
using PrKanban.Data;
using PrKanban.Models;
using PrKanban.Controllers;

namespace PrKanban.Services;

/// <summary>
/// Service for managing time entries
/// </summary>
public sealed class TimeEntryService(PrKanbanContext context) : ITimeEntryService
{
    public async System.Threading.Tasks.Task<TimeEntryResponse?> GetByIdAsync(string taskId, string timeEntryId, CancellationToken cancellationToken = default)
    {
        var entry = await context.TimeEntries
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.TaskId == taskId && e.Id == timeEntryId, cancellationToken);

        if (entry is null)
            return null;

        var user = await context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == entry.UserId, cancellationToken);

        return MapToResponse(entry, user?.Email);
    }

    public async System.Threading.Tasks.Task<IReadOnlyList<TimeEntryResponse>> GetByTaskIdAsync(string taskId, CancellationToken cancellationToken = default)
    {
        var entries = await context.TimeEntries
            .AsNoTracking()
            .Where(e => e.TaskId == taskId)
            .OrderByDescending(e => e.StartTime)
            .ToListAsync(cancellationToken);

        // Load all users for this batch of entries
        var userIds = entries.Select(e => e.UserId).Distinct().ToList();
        var users = await context.Users
            .AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Email, cancellationToken);

        return entries.Select(e => MapToResponse(e, users.TryGetValue(e.UserId, out var email) ? email : null)).ToList().AsReadOnly();
    }

    public async System.Threading.Tasks.Task<TimeEntryResponse> CreateAsync(string taskId, string userId, CreateTimeEntryRequest request, CancellationToken cancellationToken = default)
    {
        // Verify task exists
        var taskExists = await context.Tasks.AnyAsync(t => t.Id == taskId, cancellationToken);
        if (!taskExists)
            throw new KeyNotFoundException($"Task {taskId} not found");

        // Get user and verify exists
        var user = await context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
            throw new KeyNotFoundException($"User {userId} not found");

        // Calculate duration if not provided
        double durationHours = request.DurationHours ?? 0;
        if (request.EndTime.HasValue && !request.DurationHours.HasValue)
        {
            var duration = request.EndTime.Value - request.StartTime;
            durationHours = duration.TotalHours;
        }

        if (durationHours <= 0)
            throw new ArgumentException("Duration must be greater than 0 hours");

        var entry = new TimeEntry
        {
            TaskId = taskId,
            UserId = userId,
            StartTime = request.StartTime.UtcDateTime,
            EndTime = request.EndTime?.UtcDateTime,
            DurationHours = durationHours,
            Comment = request.Comment,
            IsBillable = request.IsBillable,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.TimeEntries.Add(entry);
        await context.SaveChangesAsync(cancellationToken);

        return MapToResponse(entry, user.Email);
    }

    public async System.Threading.Tasks.Task<TimeEntryResponse> UpdateAsync(string taskId, string timeEntryId, UpdateTimeEntryRequest request, CancellationToken cancellationToken = default)
    {
        var entry = await context.TimeEntries
            .FirstOrDefaultAsync(e => e.TaskId == taskId && e.Id == timeEntryId, cancellationToken);

        if (entry is null)
            throw new KeyNotFoundException($"Time entry {timeEntryId} not found for task {taskId}");

        if (entry.Locked)
            throw new InvalidOperationException($"Time entry {timeEntryId} is locked");

        if (request.StartTime.HasValue)
            entry.StartTime = request.StartTime.Value.UtcDateTime;

        if (request.EndTime.HasValue)
            entry.EndTime = request.EndTime.Value.UtcDateTime;

        if (request.DurationHours.HasValue)
        {
            if (request.DurationHours <= 0)
                throw new ArgumentException("Duration must be greater than 0 hours");
            entry.DurationHours = request.DurationHours.Value;
        }

        if (request.Comment is not null)
            entry.Comment = request.Comment;

        if (request.IsBillable.HasValue)
            entry.IsBillable = request.IsBillable.Value;

        entry.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken);

        // Load user email
        var user = await context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == entry.UserId, cancellationToken);

        return MapToResponse(entry, user?.Email);
    }

    public async System.Threading.Tasks.Task DeleteAsync(string taskId, string timeEntryId, CancellationToken cancellationToken = default)
    {
        var entry = await context.TimeEntries
            .FirstOrDefaultAsync(e => e.TaskId == taskId && e.Id == timeEntryId, cancellationToken);

        if (entry is null)
            throw new KeyNotFoundException($"Time entry {timeEntryId} not found for task {taskId}");

        if (entry.Locked)
            throw new InvalidOperationException($"Time entry {timeEntryId} is locked");

        context.TimeEntries.Remove(entry);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async System.Threading.Tasks.Task<TimeEntryResponse> SetLockedAsync(string taskId, string timeEntryId, bool locked, CancellationToken cancellationToken = default)
    {
        var entry = await context.TimeEntries
            .FirstOrDefaultAsync(e => e.TaskId == taskId && e.Id == timeEntryId, cancellationToken);

        if (entry is null)
            throw new KeyNotFoundException($"Time entry {timeEntryId} not found for task {taskId}");

        entry.Locked = locked;
        entry.LockedAt = locked ? DateTime.UtcNow : null;
        if (!locked)
            entry.LockedPeriod = null;

        await context.SaveChangesAsync(cancellationToken);

        var user = await context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == entry.UserId, cancellationToken);

        return MapToResponse(entry, user?.Email);
    }

    public async System.Threading.Tasks.Task<int> LockPeriodAsync(LockPeriodRequest request, CancellationToken cancellationToken = default)
    {
        var from = request.From.UtcDateTime;
        var to = request.To.UtcDateTime;
        var taskIds = request.TaskIds.Distinct().ToList();

        var query = context.TimeEntries
            .Where(e => taskIds.Contains(e.TaskId) && !e.Locked && e.StartTime >= from && e.StartTime <= to);
        if (!string.IsNullOrEmpty(request.UserId))
            query = query.Where(e => e.UserId == request.UserId);

        var entries = await query.ToListAsync(cancellationToken);
        var now = DateTime.UtcNow;
        foreach (var entry in entries)
        {
            entry.Locked = true;
            entry.LockedAt = now;
            entry.LockedPeriod = request.LockedPeriod;
        }

        await context.SaveChangesAsync(cancellationToken);
        return entries.Count;
    }

    private static TimeEntryResponse MapToResponse(TimeEntry entry, string? userEmail = null)
    {
        return new TimeEntryResponse(
            entry.Id,
            entry.TaskId,
            entry.UserId,
            userEmail,
            new DateTimeOffset(entry.StartTime, TimeSpan.Zero),
            entry.EndTime.HasValue ? new DateTimeOffset(entry.EndTime.Value, TimeSpan.Zero) : null,
            entry.DurationHours,
            entry.Comment,
            entry.IsBillable,
            entry.Locked,
            entry.LockedAt.HasValue ? new DateTimeOffset(entry.LockedAt.Value, TimeSpan.Zero) : null,
            entry.LockedPeriod,
            new DateTimeOffset(entry.CreatedAt, TimeSpan.Zero),
            new DateTimeOffset(entry.UpdatedAt, TimeSpan.Zero)
        );
    }
}
