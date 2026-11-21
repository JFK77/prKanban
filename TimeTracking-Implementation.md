# Time Tracking Feature Implementation

## Overview
Implemented a lightweight time tracking system for prKanban that allows users to log time against tasks. The system supports flexible time entry with optional billable/non-billable categorization.

## Database Changes

### TimeEntry Model
Created new `TimeEntry` entity in [Models/TimeEntry.cs](/C:/prKanban-migration/Backend/PrKanban.Backend/Models/TimeEntry.cs) with:
- **Id**: Unique identifier (32-char GUID)
- **TaskId**: Reference to the task being tracked
- **UserId**: User who logged the time (the recorder)
- **StartTime**: UTC timestamp for work start
- **EndTime**: UTC timestamp for work end (optional)
- **DurationHours**: Hours worked (calculated or direct entry)
- **Comment**: Optional note describing the work
- **IsBillable**: Toggle for billable vs non-billable time (default: true)
- **CreatedAt/UpdatedAt**: Audit timestamps

### Task Model Enhancement
Added to [Models/Task.cs](/C:/prKanban-migration/Backend/PrKanban.Backend/Models/Task.cs):
- **EstimatedHours**: Optional estimate for task completion (nullable double)
- **TimeEntries**: Navigation collection for all time entries logged to this task

### Database Configuration
Updated [Data/PrKanbanContext.cs](/C:/prKanban-migration/Backend/PrKanban.Backend/Data/PrKanbanContext.cs):
- Registered TimeEntry DbSet
- Configured TimeEntry entity mapping with:
  - Foreign key relationship to Task (cascade delete)
  - Foreign key relationship to ApplicationUser (restrict delete)
  - Indexes on TaskId and UserId for query performance
  - Default constraint for IsBillable = true

### Migration
Created migration [Migrations/20261121000820_AddTimeTracking.cs](/C:/prKanban-migration/Backend/PrKanban.Backend/Migrations/20261121000820_AddTimeTracking.cs)
- Creates TimeEntries table with proper schema
- Adds indexes for performance optimization
- Sets up foreign key constraints

## API Layer

### DTOs
Created [Controllers/TimeEntryDtos.cs](/C:/prKanban-migration/Backend/PrKanban.Backend/Controllers/TimeEntryDtos.cs) with:
- **CreateTimeEntryRequest**: For creating new time entries
  - Flexible duration: `EndTime` or `DurationHours` (or both)
  - Optional comment and IsBillable flag
- **UpdateTimeEntryRequest**: For partial updates
- **TimeEntryResponse**: For API responses with DateTimeOffset (ISO 8601)
- **TimeEntryListResponse**: For collection responses

### Service Layer
Created [Services/ITimeEntryService.cs](/C:/prKanban-migration/Backend/PrKanban.Backend/Services/ITimeEntryService.cs) interface with operations:
- `GetByIdAsync()`: Get specific time entry
- `GetByTaskIdAsync()`: Get all entries for a task (sorted by recency)
- `CreateAsync()`: Create new time entry with validation
- `UpdateAsync()`: Update time entry details
- `DeleteAsync()`: Remove time entry

Implemented in [Services/TimeEntryService.cs](/C:/prKanban-migration/Backend/PrKanban.Backend/Services/TimeEntryService.cs):
- Full CRUD operations
- Automatic duration calculation from start/end times
- Validation for non-zero durations
- Task and user existence verification
- Uses DateTimeOffset for proper timezone handling

### API Endpoints
Implemented in [Controllers/TimeEntryEndpoints.cs](/C:/prKanban-migration/Backend/PrKanban.Backend/Controllers/TimeEntryEndpoints.cs):

```
GET    /api/tasks/{taskId}/time-entries
GET    /api/tasks/{taskId}/time-entries/{timeEntryId}
POST   /api/tasks/{taskId}/time-entries
PUT    /api/tasks/{taskId}/time-entries/{timeEntryId}
DELETE /api/tasks/{taskId}/time-entries/{timeEntryId}
```

All endpoints:
- Require authorization
- Include OpenAPI documentation
- Return proper HTTP status codes (200, 201, 204, 400, 404)
- Include `Location` header for POST responses
- Use TypedResults for compile-time type safety

## Configuration

### Program.cs Updates
Registered service and endpoints:
```csharp
builder.Services.AddScoped<ITimeEntryService, TimeEntryService>();
app.MapTimeEntryEndpoints();
```

## Testing
Created [TimeEntries.http](/C:/prKanban-migration/Backend/PrKanban.Backend/TimeEntries.http) with examples for:
- Getting all time entries
- Getting specific entry
- Creating entries with start/end times
- Creating entries with direct duration
- Creating non-billable entries
- Updating entries
- Deleting entries

## Usage Examples

### Log time with start and end times
```json
POST /api/tasks/{taskId}/time-entries
{
  "startTime": "2024-01-15T09:00:00Z",
  "endTime": "2024-01-15T10:30:00Z",
  "comment": "Analysis and design",
  "isBillable": true
}
```

### Log time with direct duration
```json
POST /api/tasks/{taskId}/time-entries
{
  "startTime": "2024-01-15T14:00:00Z",
  "durationHours": 1.5,
  "comment": "Implementation",
  "isBillable": true
}
```

### Non-billable time entry
```json
POST /api/tasks/{taskId}/time-entries
{
  "startTime": "2024-01-15T11:00:00Z",
  "durationHours": 0.5,
  "comment": "Team meeting",
  "isBillable": false
}
```

## Key Features
✅ Flexible time logging (start/end or duration)
✅ Billable/non-billable categorization
✅ Optional comments for context
✅ User-aware time tracking (tracks who logged)
✅ Full audit trail (created/updated timestamps)
✅ Query optimization with indexes
✅ Type-safe API responses with DateTimeOffset
✅ Comprehensive validation
✅ RESTful design with proper HTTP semantics

## Future Enhancements
Potential additions for later:
- Time entry approval workflow
- Bulk time entry operations
- Time entry reporting and analytics
- Integration with project budgets for burn-down
- Time entry templates for common tasks
- Recurring time entries
- Time tracking dashboard KPIs
