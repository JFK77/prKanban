using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using PrKanban.Controllers;
using PrKanban.Services;

namespace PrKanban.Controllers;

/// <summary>
/// Endpoints for managing task time entries
/// </summary>
public static class TimeEntryEndpoints
{
    /// <summary>
    /// Map time entry endpoints
    /// </summary>
    public static void MapTimeEntryEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/tasks/{taskId}/time-entries")
            .WithTags("Time Entries")
            .RequireAuthorization();

        group.MapGet("", GetTimeEntries)
            .WithName("GetTimeEntries")
            .WithSummary("Get all time entries for a task")
            .WithDescription("Returns all time entries logged against a specific task, sorted by most recent first.")
            .Produces<TimeEntryListResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("{timeEntryId}", GetTimeEntry)
            .WithName("GetTimeEntry")
            .WithSummary("Get a specific time entry")
            .WithDescription("Returns details of a specific time entry for a task.")
            .Produces<TimeEntryResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("", CreateTimeEntry)
            .WithName("CreateTimeEntry")
            .WithSummary("Create a new time entry")
            .WithDescription("Log time against a task. Duration can be specified via EndTime or DurationHours.")
            .Produces<TimeEntryResponse>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPut("{timeEntryId}", UpdateTimeEntry)
            .WithName("UpdateTimeEntry")
            .WithSummary("Update a time entry")
            .WithDescription("Modify the details of an existing time entry.")
            .Produces<TimeEntryResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPut("{timeEntryId}/lock", SetTimeEntryLock)
            .WithName("SetTimeEntryLock")
            .WithSummary("Lock or unlock a time entry")
            .WithDescription("Locked entries cannot be edited or deleted until they are unlocked.")
            .Produces<TimeEntryResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        group.MapDelete("{timeEntryId}", DeleteTimeEntry)
            .WithName("DeleteTimeEntry")
            .WithSummary("Delete a time entry")
            .WithDescription("Remove a time entry from a task.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict);

        app.MapPost("/api/time-entries/lock-period", LockPeriod)
            .WithTags("Time Entries")
            .RequireAuthorization()
            .WithName("LockTimeEntryPeriod")
            .WithSummary("Lock all time entries of a billing period")
            .WithDescription("Locks every unlocked time entry of the given tasks whose start time lies within the period.")
            .Produces<LockPeriodResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);
    }

    private static async System.Threading.Tasks.Task<Results<Ok<TimeEntryListResponse>, NotFound>> GetTimeEntries(
        string taskId,
        ITimeEntryService service,
        CancellationToken cancellationToken)
    {
        var entries = await service.GetByTaskIdAsync(taskId, cancellationToken);
        return TypedResults.Ok(new TimeEntryListResponse(entries));
    }

    private static async System.Threading.Tasks.Task<Results<Ok<TimeEntryResponse>, NotFound>> GetTimeEntry(
        string taskId,
        string timeEntryId,
        ITimeEntryService service,
        CancellationToken cancellationToken)
    {
        var entry = await service.GetByIdAsync(taskId, timeEntryId, cancellationToken);
        return entry is null ? TypedResults.NotFound() : TypedResults.Ok(entry);
    }

    private static async System.Threading.Tasks.Task<Results<Created<TimeEntryResponse>, BadRequest, NotFound>> CreateTimeEntry(
        string taskId,
        CreateTimeEntryRequest request,
        ITimeEntryService service,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = httpContext.User.FindFirst("sub")?.Value
                ?? httpContext.User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value
                ?? throw new InvalidOperationException("User ID not found in claims");

            var entry = await service.CreateAsync(taskId, userId, request, cancellationToken);
            return TypedResults.Created($"/api/tasks/{taskId}/time-entries/{entry.Id}", entry);
        }
        catch (KeyNotFoundException)
        {
            return TypedResults.NotFound();
        }
        catch (ArgumentException)
        {
            return TypedResults.BadRequest();
        }
    }

    private static async System.Threading.Tasks.Task<Results<Ok<TimeEntryResponse>, BadRequest, NotFound, Conflict>> UpdateTimeEntry(
        string taskId,
        string timeEntryId,
        UpdateTimeEntryRequest request,
        ITimeEntryService service,
        CancellationToken cancellationToken)
    {
        try
        {
            var entry = await service.UpdateAsync(taskId, timeEntryId, request, cancellationToken);
            return TypedResults.Ok(entry);
        }
        catch (KeyNotFoundException)
        {
            return TypedResults.NotFound();
        }
        catch (InvalidOperationException)
        {
            return TypedResults.Conflict();
        }
        catch (ArgumentException)
        {
            return TypedResults.BadRequest();
        }
    }

    private static async System.Threading.Tasks.Task<Results<NoContent, NotFound, Conflict>> DeleteTimeEntry(
        string taskId,
        string timeEntryId,
        ITimeEntryService service,
        CancellationToken cancellationToken)
    {
        try
        {
            await service.DeleteAsync(taskId, timeEntryId, cancellationToken);
            return TypedResults.NoContent();
        }
        catch (KeyNotFoundException)
        {
            return TypedResults.NotFound();
        }
        catch (InvalidOperationException)
        {
            return TypedResults.Conflict();
        }
    }

    private static async System.Threading.Tasks.Task<Results<Ok<TimeEntryResponse>, NotFound>> SetTimeEntryLock(
        string taskId,
        string timeEntryId,
        SetTimeEntryLockRequest request,
        ITimeEntryService service,
        CancellationToken cancellationToken)
    {
        try
        {
            var entry = await service.SetLockedAsync(taskId, timeEntryId, request.Locked, cancellationToken);
            return TypedResults.Ok(entry);
        }
        catch (KeyNotFoundException)
        {
            return TypedResults.NotFound();
        }
    }

    private static async System.Threading.Tasks.Task<Results<Ok<LockPeriodResponse>, BadRequest>> LockPeriod(
        LockPeriodRequest request,
        ITimeEntryService service,
        CancellationToken cancellationToken)
    {
        if (request.TaskIds is not { Length: > 0 and <= 10000 } || request.From > request.To)
            return TypedResults.BadRequest();
        var count = await service.LockPeriodAsync(request, cancellationToken);
        return TypedResults.Ok(new LockPeriodResponse(count));
    }
}
