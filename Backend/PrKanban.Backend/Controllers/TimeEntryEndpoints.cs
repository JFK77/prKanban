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

        group.MapDelete("{timeEntryId}", DeleteTimeEntry)
            .WithName("DeleteTimeEntry")
            .WithSummary("Delete a time entry")
            .WithDescription("Remove a time entry from a task.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);
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

    private static async System.Threading.Tasks.Task<Results<Ok<TimeEntryResponse>, BadRequest, NotFound>> UpdateTimeEntry(
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
        catch (ArgumentException)
        {
            return TypedResults.BadRequest();
        }
    }

    private static async System.Threading.Tasks.Task<Results<NoContent, NotFound>> DeleteTimeEntry(
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
    }
}
