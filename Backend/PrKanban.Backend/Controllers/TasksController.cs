using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using PrKanban.Data;
using PrKanban.Models;
using PrKanban.Services;
using Task = PrKanban.Models.Task;

namespace PrKanban.Controllers
{
    /// <summary>
    /// Task management endpoints (CRUD operations)
    /// </summary>
    [ApiController]
    [Route("api/v1/tasks")]
    [Authorize(Roles = "Admin,User")]
    public class TasksController : ControllerBase
    {
        private readonly PrKanbanContext _context;
        private readonly ILogger<TasksController> _logger;
        private readonly ITaskMoveService _taskMoves;
        private readonly IProjectBoardService _projectBoards;

        public TasksController(
            PrKanbanContext context,
            ILogger<TasksController> logger,
            ITaskMoveService taskMoves,
            IProjectBoardService projectBoards)
        {
            _context = context;
            _logger = logger;
            _taskMoves = taskMoves;
            _projectBoards = projectBoards;
        }

        /// <summary>Moves a task within the shared board and persists the target lane order.</summary>
        [HttpPut("{id}/position")]
        public async Task<IActionResult> MoveTask(
            string id, [FromBody] MoveTaskRequest request, CancellationToken cancellationToken)
        {
            var result = await _taskMoves.MoveAsync(
                id, request.ColumnName, request.SubColumnName, request.BeforeTaskId, cancellationToken);
            return result switch
            {
                TaskMoveResult.Success => NoContent(),
                TaskMoveResult.TaskNotFound => NotFound(new ProblemDetails { Status = 404, Title = "Task not found." }),
                TaskMoveResult.InvalidTarget => BadRequest(new ProblemDetails { Status = 400, Title = "Invalid target lane." }),
                TaskMoveResult.InvalidBeforeTask => Conflict(new ProblemDetails { Status = 409, Title = "The target position is no longer available." }),
                _ => throw new InvalidOperationException("Unexpected task move result.")
            };
        }

        /// <summary>
        /// GET /api/v1/tasks - Get all tasks visible to administrators and users
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetTasks(
            [FromQuery] bool? archived,
            [FromQuery] string? column,
            [FromQuery] string? owner,
            [FromQuery] int skip = 0,
            [FromQuery] int take = 100)
        {
            var query = _context.Tasks.Include(t => t.Ordering)
                .Include(t => t.StatusHistory)
                .Include(t => t.DeliverableAssignment).ThenInclude(a => a!.Deliverable)
                .ThenInclude(d => d.Project).ThenInclude(p => p.Customer)
                .Include(t => t.ProjectBoardPosition).AsQueryable();

            // Apply optional filters
            if (archived.HasValue)
                query = query.Where(t => t.IsArchived == archived.Value);

            if (!string.IsNullOrEmpty(column))
                query = query.Where(t => t.ColumnName == column);

            if (!string.IsNullOrEmpty(owner))
                query = query.Where(t => t.Owner == owner);

            var total = await query.CountAsync();
            var tasks = await query
                .OrderByDescending(t => t.UpdatedAt)
                .ThenBy(t => t.Id)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
            await PopulateHierarchyOrdersAsync(tasks);

            return Ok(new
            {
                success = true,
                data = new
                {
                    tasks = tasks,
                    pagination = new
                    {
                        total,
                        returned = tasks.Count,
                        skip,
                        take
                    }
                }
            });
        }

        /// <summary>
        /// GET /api/v1/tasks/:id - Get single task
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetTask(string id)
        {
            var task = await _context.Tasks
                .Include(t => t.Ordering)
                .Include(t => t.StatusHistory)
                .Include(t => t.DeliverableAssignment).ThenInclude(a => a!.Deliverable)
                .ThenInclude(d => d.Project).ThenInclude(p => p.Customer)
                .Include(t => t.ProjectBoardPosition)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null)
                return NotFound(new { success = false, error = new { code = "NOT_FOUND", message = "Task not found" } });

            await PopulateHierarchyOrdersAsync(new[] { task });
            return Ok(new { success = true, data = task });
        }

        /// <summary>
        /// POST /api/v1/tasks - Create new task
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateTask([FromBody] CreateTaskRequest request, CancellationToken cancellationToken)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            if (string.IsNullOrWhiteSpace(request.Title))
                return BadRequest(new { success = false, error = new { code = "INVALID_REQUEST", message = "Title is required" } });

            var deliverable = await _context.Deliverables
                .Include(d => d.Project).ThenInclude(p => p.Customer)
                .Include(d => d.Project).ThenInclude(p => p.Details)
                .FirstOrDefaultAsync(d => d.Id == request.DeliverableId, cancellationToken);
            if (deliverable is null)
                return BadRequest(new { success = false, error = new { code = "INVALID_DELIVERABLE", message = "A valid deliverable is required." } });
            if (string.IsNullOrEmpty(deliverable.Project.Details?.ShortKey))
                return BadRequest(new { success = false, error = new { code = "MISSING_PROJECT_KEY", message = "Set a project short key before creating tasks." } });

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            await _context.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE ProjectDetails SET LastTaskNumber = LastTaskNumber + 1 WHERE ProjectId = {deliverable.ProjectId}",
                cancellationToken);
            var taskNumberSequence = await _context.Database
                .SqlQuery<int>($"SELECT LastTaskNumber AS Value FROM ProjectDetails WHERE ProjectId = {deliverable.ProjectId}")
                .SingleAsync(cancellationToken);

            var hierarchyOrder = (await _context.HierarchyPositions
                .Where(p => p.ItemKind == "tasks" && p.ParentId == deliverable.Id)
                .Select(p => (int?)p.SortOrder)
                .MaxAsync(cancellationToken)) ?? -1;
            var masterColumnName = MasterBoardColumns.Normalize(request.ColumnName);
            ProjectBoardColumn? requestedProjectColumn = null;
            if (!string.IsNullOrWhiteSpace(request.ProjectBoardColumnId))
            {
                requestedProjectColumn = await _context.ProjectBoardColumns.FirstOrDefaultAsync(
                    column => column.Id == request.ProjectBoardColumnId && column.ProjectId == deliverable.ProjectId,
                    cancellationToken);
                if (requestedProjectColumn is null)
                    return BadRequest(new ProblemDetails { Status = 400, Title = "The selected project-board column is invalid." });
                masterColumnName = requestedProjectColumn.MasterColumnName;
            }
            var task = new Task
            {
                Id = $"{deliverable.Project.Details.ShortKey}-T-{Guid.NewGuid():N}",
                TaskNumber = $"{deliverable.Project.Details.ShortKey}-{taskNumberSequence}",
                Title = request.Title,
                TaskType = request.TaskType ?? "task",
                Owner = string.IsNullOrWhiteSpace(request.Owner) ? deliverable.Project.Details.DefaultAssignee : request.Owner,
                Customer = request.Customer,
                Description = TaskDescriptionSanitizer.Sanitize(request.Description),
                ColumnName = masterColumnName,
                SubColumnName = masterColumnName == MasterBoardColumns.Backlog
                    ? MasterBoardColumns.Backlog
                    : request.SubColumnName is "Doing" or "Done" ? request.SubColumnName : "Doing",
                UserId = userId,
                Tags = request.Tags ?? Array.Empty<string>(),
                Checklists = request.Checklists ?? new List<TaskChecklist>(),
                EstimatedHours = NormalizeHours(request.EstimatedHours),
                BudgetHours = NormalizeHours(request.BudgetHours),
                Ordering = request.Order.HasValue ? new TaskOrder { Value = request.Order.Value } : null,
                DeliverableAssignment = new TaskDeliverable { DeliverableId = deliverable.Id, Deliverable = deliverable, SortOrder = hierarchyOrder + 1 },
                ProjectBoardPosition = requestedProjectColumn is null
                    ? null
                    : new ProjectBoardTaskPosition
                    {
                        ProjectBoardColumnId = requestedProjectColumn.Id,
                        SortOrder = ((await _context.ProjectBoardTaskPositions
                            .Where(position => position.ProjectBoardColumnId == requestedProjectColumn.Id)
                            .Select(position => (double?)position.SortOrder)
                            .MaxAsync(cancellationToken)) ?? -1024d) + 1024d
                    }
            };
            if (requestedProjectColumn is null)
                await _projectBoards.EnsureTaskPositionAsync(task, deliverable.ProjectId, masterColumnName, cancellationToken);

            _context.Tasks.Add(task);
            _context.HierarchyPositions.Add(new HierarchyPosition
            {
                ItemKind = "tasks",
                ItemId = task.Id,
                ParentId = deliverable.Id,
                SortOrder = hierarchyOrder + 1
            });
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            _logger.LogInformation($"Task {task.Id} created by user {userId}");

            return CreatedAtAction(nameof(GetTask), new { id = task.Id }, new { success = true, data = task });
        }

        /// <summary>
        /// PUT /api/v1/tasks/:id - Update task
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTask(
            string id, [FromBody] UpdateTaskRequest request, CancellationToken cancellationToken)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var task = await _context.Tasks
                .Include(t => t.Ordering)
                .Include(t => t.DeliverableAssignment)
                    .ThenInclude(assignment => assignment!.Deliverable)
                .Include(t => t.ProjectBoardPosition)
                .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

            if (task == null)
                return NotFound(new { success = false, error = new { code = "NOT_FOUND", message = "Task not found" } });

            var previousProjectId = task.DeliverableAssignment?.Deliverable?.ProjectId;
            var previousMasterColumn = MasterBoardColumns.Normalize(task.ColumnName);
            var projectChanged = false;
            if (request.DeliverableId != null)
            {
                var targetDeliverable = string.IsNullOrWhiteSpace(request.DeliverableId)
                    ? null
                    : await _context.Deliverables.FirstOrDefaultAsync(
                        deliverable => deliverable.Id == request.DeliverableId, cancellationToken);
                if (targetDeliverable is null)
                    return BadRequest(new { success = false, error = new { code = "INVALID_DELIVERABLE", message = "A valid deliverable is required." } });
                if (task.DeliverableAssignment?.DeliverableId != request.DeliverableId)
                {
                    var hierarchyOrder = (await _context.HierarchyPositions
                        .Where(p => p.ItemKind == "tasks" && p.ParentId == request.DeliverableId)
                        .Select(p => (int?)p.SortOrder)
                        .MaxAsync(cancellationToken)) ?? -1;
                    projectChanged = previousProjectId != targetDeliverable.ProjectId;
                    var previousDeliverableId = task.DeliverableAssignment?.DeliverableId;
                    if (previousDeliverableId is not null)
                    {
                        var previousPositions = await _context.HierarchyPositions
                            .Where(p => p.ItemKind == "tasks" && p.ParentId == previousDeliverableId && p.ItemId != task.Id)
                            .OrderBy(p => p.SortOrder)
                            .ThenBy(p => p.ItemId)
                            .ToListAsync(cancellationToken);
                        for (var index = 0; index < previousPositions.Count; index++)
                            previousPositions[index].SortOrder = index;
                    }
                    var position = await _context.HierarchyPositions
                        .FirstOrDefaultAsync(p => p.ItemKind == "tasks" && p.ItemId == task.Id, cancellationToken);
                    if (position is null)
                    {
                        position = new HierarchyPosition { ItemKind = "tasks", ItemId = task.Id };
                        _context.HierarchyPositions.Add(position);
                    }
                    position.ParentId = request.DeliverableId;
                    position.SortOrder = hierarchyOrder + 1;
                    if (task.DeliverableAssignment is null)
                    {
                        task.DeliverableAssignment = new TaskDeliverable
                        {
                            TaskId = task.Id,
                            DeliverableId = targetDeliverable.Id,
                            Deliverable = targetDeliverable,
                            SortOrder = hierarchyOrder + 1
                        };
                        _context.TaskDeliverables.Add(task.DeliverableAssignment);
                    }
                    else
                    {
                        task.DeliverableAssignment.DeliverableId = targetDeliverable.Id;
                        task.DeliverableAssignment.Deliverable = targetDeliverable;
                        task.DeliverableAssignment.SortOrder = hierarchyOrder + 1;
                    }
                }
            }

            if (!string.IsNullOrEmpty(request.Title))
                task.Title = request.Title;
            if (!string.IsNullOrEmpty(request.TaskType))
                task.TaskType = request.TaskType;
            if (!string.IsNullOrEmpty(request.Owner))
                task.Owner = request.Owner;
            if (!string.IsNullOrEmpty(request.Customer))
                task.Customer = request.Customer;
            if (request.Description != null)
                task.Description = TaskDescriptionSanitizer.Sanitize(request.Description);
            if (!string.IsNullOrEmpty(request.ColumnName))
                task.ColumnName = MasterBoardColumns.Normalize(request.ColumnName);
            if (request.SubColumnName != null)
                task.SubColumnName = request.SubColumnName;
            else if (task.ColumnName == MasterBoardColumns.Backlog)
                task.SubColumnName = MasterBoardColumns.Backlog;
            else if (task.SubColumnName is not ("Doing" or "Done"))
                task.SubColumnName = "Doing";
            var masterColumnChanged = previousMasterColumn != MasterBoardColumns.Normalize(task.ColumnName);
            if (task.DeliverableAssignment is not null && (projectChanged || masterColumnChanged))
            {
                var projectId = await _context.Deliverables
                    .Where(deliverable => deliverable.Id == task.DeliverableAssignment.DeliverableId)
                    .Select(deliverable => deliverable.ProjectId)
                    .FirstAsync(cancellationToken);
                await _projectBoards.EnsureTaskPositionAsync(task, projectId, task.ColumnName, cancellationToken);
            }
            if (request.Tags != null)
                task.Tags = request.Tags;
            if (request.Checklists != null)
                task.Checklists = request.Checklists;
            if (request.EstimatedHours.HasValue)
                task.EstimatedHours = NormalizeHours(request.EstimatedHours);
            if (request.BudgetHours.HasValue)
                task.BudgetHours = NormalizeHours(request.BudgetHours);
            if (request.Order.HasValue)
            {
                if (task.Ordering == null)
                    task.Ordering = new TaskOrder { TaskId = task.Id, Value = request.Order.Value };
                else
                    task.Ordering.Value = request.Order.Value;
            }

            await _context.SaveChangesAsync(cancellationToken);
            
            // Reload the task to get updated values
            task = await _context.Tasks.Include(t => t.Ordering)
                .Include(t => t.StatusHistory)
                .Include(t => t.DeliverableAssignment).ThenInclude(a => a!.Deliverable)
                .ThenInclude(d => d.Project).ThenInclude(p => p.Customer)
                .Include(t => t.ProjectBoardPosition)
                .FirstAsync(t => t.Id == id, cancellationToken);
            await PopulateHierarchyOrdersAsync(new[] { task });
            
            _logger.LogInformation($"Task {task.Id} updated by user {userId}");

            return Ok(new { success = true, data = task });
        }

        /// <summary>Rounds an hour value to two decimals; non-positive values clear to null.</summary>
        private static double? NormalizeHours(double? hours) =>
            hours.HasValue && hours.Value > 0 ? Math.Round(hours.Value, 2) : null;

        private async System.Threading.Tasks.Task PopulateHierarchyOrdersAsync(IReadOnlyCollection<Task> tasks)
        {
            if (tasks.Count == 0) return;
            var ids = tasks.Select(task => task.Id).ToArray();
            var positions = await _context.HierarchyPositions
                .Where(position => position.ItemKind == "tasks" && ids.Contains(position.ItemId))
                .ToDictionaryAsync(position => position.ItemId, position => position.SortOrder);
            foreach (var task in tasks)
            {
                if (task.DeliverableAssignment is not null &&
                    positions.TryGetValue(task.Id, out var hierarchyOrder))
                    task.DeliverableAssignment.SortOrder = hierarchyOrder;
            }
        }

        /// <summary>
        /// DELETE /api/v1/tasks/:id - Delete task
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTask(string id)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var task = await _context.Tasks
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null)
                return NotFound(new { success = false, error = new { code = "NOT_FOUND", message = "Task not found" } });

            var position = await _context.HierarchyPositions
                .FirstOrDefaultAsync(p => p.ItemKind == "tasks" && p.ItemId == id);
            if (position is not null)
            {
                var siblings = await _context.HierarchyPositions
                    .Where(p => p.ItemKind == "tasks" && p.ParentId == position.ParentId && p.ItemId != id)
                    .OrderBy(p => p.SortOrder)
                    .ThenBy(p => p.ItemId)
                    .ToListAsync();
                for (var index = 0; index < siblings.Count; index++)
                    siblings[index].SortOrder = index;
                _context.HierarchyPositions.Remove(position);
            }
            _context.Tasks.Remove(task);
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Task {task.Id} deleted by user {userId}");

            return Ok(new { success = true, data = new { message = "Task deleted successfully", taskId = id } });
        }

        /// <summary>
        /// PUT /api/v1/tasks/:id/archive - Archive task (soft delete)
        /// </summary>
        [HttpPut("{id}/archive")]
        public async Task<IActionResult> ArchiveTask(string id)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var task = await _context.Tasks
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null)
                return NotFound(new { success = false, error = new { code = "NOT_FOUND", message = "Task not found" } });

            task.IsArchived = true;
            await _context.SaveChangesAsync();

            return Ok(new { success = true, data = task });
        }

        /// <summary>
        /// PUT /api/v1/tasks/:id/restore - Restore archived task
        /// </summary>
        [HttpPut("{id}/restore")]
        public async Task<IActionResult> RestoreTask(string id)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var task = await _context.Tasks
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null)
                return NotFound(new { success = false, error = new { code = "NOT_FOUND", message = "Task not found" } });

            task.IsArchived = false;
            await _context.SaveChangesAsync();

            return Ok(new { success = true, data = task });
        }
    }

    // Request DTOs
    /// <summary>Target lane and optional next task for a shared board move.</summary>
    public sealed record MoveTaskRequest
    {
        [Required, MaxLength(100)]
        public required string ColumnName { get; init; }

        [Required, MaxLength(100)]
        public required string SubColumnName { get; init; }

        public string? BeforeTaskId { get; init; }
    }

    public class CreateTaskRequest
    {
        public string Title { get; set; } = string.Empty;
        public string? TaskType { get; set; }
        public string? Owner { get; set; }
        public string? Customer { get; set; }
        public string? DeliverableId { get; set; }
        public string? Description { get; set; }
        public string? ColumnName { get; set; }
        public string? SubColumnName { get; set; }
        public string? ProjectBoardColumnId { get; set; }
        public string[]? Tags { get; set; }
        public List<TaskChecklist>? Checklists { get; set; }
        public double? Order { get; set; }

        /// <summary>Estimated effort in hours; 0 clears the value.</summary>
        public double? EstimatedHours { get; set; }

        /// <summary>Time budget in hours according to the order/contract; 0 clears the value.</summary>
        public double? BudgetHours { get; set; }
    }

    public class UpdateTaskRequest
    {
        public string? Title { get; set; }
        public string? TaskType { get; set; }
        public string? Owner { get; set; }
        public string? Customer { get; set; }
        public string? DeliverableId { get; set; }
        public string? Description { get; set; }
        public string? ColumnName { get; set; }
        public string? SubColumnName { get; set; }
        public string[]? Tags { get; set; }
        public List<TaskChecklist>? Checklists { get; set; }
        public double? Order { get; set; }

        /// <summary>Estimated effort in hours; 0 clears the value.</summary>
        public double? EstimatedHours { get; set; }

        /// <summary>Time budget in hours according to the order/contract; 0 clears the value.</summary>
        public double? BudgetHours { get; set; }
    }
}
