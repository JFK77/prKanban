using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrKanban.Data;
using PrKanban.Models;
using Task = PrKanban.Models.Task;

namespace PrKanban.Controllers
{
    /// <summary>
    /// Task management endpoints (CRUD operations)
    /// </summary>
    [ApiController]
    [Route("api/v1/tasks")]
    [Authorize]
    public class TasksController : ControllerBase
    {
        private readonly PrKanbanContext _context;
        private readonly ILogger<TasksController> _logger;

        public TasksController(PrKanbanContext context, ILogger<TasksController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// GET /api/v1/tasks - Get all tasks for current user with optional filtering
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetTasks(
            [FromQuery] bool? archived,
            [FromQuery] string? column,
            [FromQuery] string? owner,
            [FromQuery] int skip = 0,
            [FromQuery] int take = 100)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var query = _context.Tasks.AsQueryable();

            // Filter by user
            query = query.Where(t => t.UserId == userId);

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
                .Skip(skip)
                .Take(take)
                .ToListAsync();

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
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var task = await _context.Tasks
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (task == null)
                return NotFound(new { success = false, error = new { code = "NOT_FOUND", message = "Task not found" } });

            return Ok(new { success = true, data = task });
        }

        /// <summary>
        /// POST /api/v1/tasks - Create new task
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateTask([FromBody] CreateTaskRequest request)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            if (string.IsNullOrWhiteSpace(request.Title))
                return BadRequest(new { success = false, error = new { code = "INVALID_REQUEST", message = "Title is required" } });

            var task = new Task
            {
                Title = request.Title,
                TaskType = request.TaskType ?? "task",
                Owner = request.Owner,
                Customer = request.Customer,
                Description = request.Description,
                ColumnName = request.ColumnName ?? "todo",
                SubColumnName = request.SubColumnName,
                UserId = userId,
                Tags = request.Tags ?? Array.Empty<string>()
            };

            _context.Tasks.Add(task);
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Task {task.Id} created by user {userId}");

            return CreatedAtAction(nameof(GetTask), new { id = task.Id }, new { success = true, data = task });
        }

        /// <summary>
        /// PUT /api/v1/tasks/:id - Update task
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTask(string id, [FromBody] UpdateTaskRequest request)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var task = await _context.Tasks
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (task == null)
                return NotFound(new { success = false, error = new { code = "NOT_FOUND", message = "Task not found" } });

            if (!string.IsNullOrEmpty(request.Title))
                task.Title = request.Title;
            if (!string.IsNullOrEmpty(request.Owner))
                task.Owner = request.Owner;
            if (!string.IsNullOrEmpty(request.Customer))
                task.Customer = request.Customer;
            if (request.Description != null)
                task.Description = request.Description;
            if (!string.IsNullOrEmpty(request.ColumnName))
                task.ColumnName = request.ColumnName;
            if (request.SubColumnName != null)
                task.SubColumnName = request.SubColumnName;
            if (request.Tags != null)
                task.Tags = request.Tags;

            await _context.SaveChangesAsync();
            
            // Reload the task to get updated values
            task = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == id);
            
            _logger.LogInformation($"Task {task.Id} updated by user {userId}");

            return Ok(new { success = true, data = task });
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
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (task == null)
                return NotFound(new { success = false, error = new { code = "NOT_FOUND", message = "Task not found" } });

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
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

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
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (task == null)
                return NotFound(new { success = false, error = new { code = "NOT_FOUND", message = "Task not found" } });

            task.IsArchived = false;
            await _context.SaveChangesAsync();

            return Ok(new { success = true, data = task });
        }
    }

    // Request DTOs
    public class CreateTaskRequest
    {
        public string Title { get; set; } = string.Empty;
        public string? TaskType { get; set; }
        public string? Owner { get; set; }
        public string? Customer { get; set; }
        public string? Description { get; set; }
        public string? ColumnName { get; set; }
        public string? SubColumnName { get; set; }
        public string[]? Tags { get; set; }
    }

    public class UpdateTaskRequest
    {
        public string? Title { get; set; }
        public string? Owner { get; set; }
        public string? Customer { get; set; }
        public string? Description { get; set; }
        public string? ColumnName { get; set; }
        public string? SubColumnName { get; set; }
        public string[]? Tags { get; set; }
    }
}
