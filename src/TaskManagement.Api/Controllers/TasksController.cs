using Microsoft.AspNetCore.Mvc;
using TaskManagement.Api.Http;
using TaskManagement.Application.DTOs;
using TaskManagement.Application.Interfaces;

namespace TaskManagement.Api.Controllers;

/// <summary>
/// Manages tasks: create, read, list with filters, search, update, delete and status history.
/// </summary>
[ApiController]
[Route("api/tasks")]
[Produces("application/json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
public sealed class TasksController : ControllerBase
{
    private readonly ITaskService _taskService;

    public TasksController(ITaskService taskService)
    {
        _taskService = taskService;
    }

    /// <summary>
    /// Lists tasks, optionally filtered by status, due date and/or overdue state.
    /// </summary>
    /// <remarks>
    /// Filters are optional and combined with AND. Results are ordered by due date
    /// (tasks without a due date last), then by creation time.
    ///
    ///     GET /api/tasks?status=Pendente&amp;dueDate=2026-10-01
    ///     GET /api/tasks?overdue=true
    /// </remarks>
    /// <param name="filter">Optional filters.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <response code="200">The tasks that match the filters (possibly empty).</response>
    /// <response code="400">An invalid status, date or overdue value was provided.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<TaskResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<TaskResponse>>> List(
        [FromQuery] TaskFilterRequest filter,
        CancellationToken cancellationToken)
    {
        return Ok(await _taskService.ListAsync(filter, cancellationToken));
    }

    /// <summary>
    /// Searches tasks by a term contained in the title or description.
    /// </summary>
    /// <remarks>
    /// The match is a case-insensitive "contains" on title and description.
    ///
    ///     GET /api/tasks/search?term=coleta
    /// </remarks>
    /// <param name="term" example="coleta">Text to look for in the title or description.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <response code="200">The matching tasks (empty when nothing matches).</response>
    /// <response code="400">The search term is missing or empty.</response>
    [HttpGet("search")]
    [ProducesResponseType(typeof(IReadOnlyList<TaskResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<TaskResponse>>> Search(
        [FromQuery] string? term,
        CancellationToken cancellationToken)
    {
        return Ok(await _taskService.SearchAsync(term, cancellationToken));
    }

    /// <summary>
    /// Gets a task by its identifier.
    /// </summary>
    /// <remarks>
    /// The ETag header carries the task version; send it back in If-Match on PUT/DELETE
    /// to avoid overwriting changes made by someone else.
    /// </remarks>
    /// <param name="id">Task identifier.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <response code="200">The task.</response>
    /// <response code="404">No task has this identifier.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var task = await _taskService.GetByIdAsync(id, cancellationToken);
        SetETag(task);
        return Ok(task);
    }

    /// <summary>
    /// Gets the status history of a task, oldest first.
    /// </summary>
    /// <remarks>
    /// The first entry is the status the task was created with (fromStatus = null);
    /// every later status change adds an entry.
    /// </remarks>
    /// <param name="id">Task identifier.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <response code="200">The status changes of the task.</response>
    /// <response code="404">No task has this identifier.</response>
    [HttpGet("{id:guid}/history")]
    [ProducesResponseType(typeof(IReadOnlyList<TaskStatusChangeResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<TaskStatusChangeResponse>>> GetHistory(
        Guid id,
        CancellationToken cancellationToken)
    {
        return Ok(await _taskService.GetHistoryAsync(id, cancellationToken));
    }

    /// <summary>
    /// Creates a task.
    /// </summary>
    /// <remarks>
    ///     POST /api/tasks
    ///     {
    ///       "title": "Conferir carga do pedido 4521",
    ///       "description": "Validar volumes e lacres antes da expedição",
    ///       "dueDate": "2026-10-01",
    ///       "status": "Pendente"
    ///     }
    /// </remarks>
    /// <param name="request">Task data.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <response code="201">The created task. The Location header points to the new resource.</response>
    /// <response code="400">The request contains invalid data.</response>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TaskResponse>> Create(
        [FromBody] CreateTaskRequest request,
        CancellationToken cancellationToken)
    {
        var created = await _taskService.CreateAsync(request, cancellationToken);
        SetETag(created);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>
    /// Replaces the editable data of a task (title, description, due date and status).
    /// </summary>
    /// <remarks>
    /// Optionally send the task's ETag in If-Match: if the task changed in the meantime,
    /// the update is rejected with 412 instead of silently overwriting the other change.
    ///
    ///     PUT /api/tasks/3fa85f64-5717-4562-b3fc-2c963f66afa6
    ///     If-Match: "1"
    ///     {
    ///       "title": "Conferir carga do pedido 4521",
    ///       "description": "Volumes conferidos; aguardando coleta da transportadora",
    ///       "dueDate": "2026-10-02",
    ///       "status": "EmProgresso"
    ///     }
    /// </remarks>
    /// <param name="id">Task identifier.</param>
    /// <param name="request">New task data.</param>
    /// <param name="ifMatch">Optional ETag previously returned for this task (e.g. "1").</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <response code="200">The updated task, with its new ETag.</response>
    /// <response code="400">The request contains invalid data.</response>
    /// <response code="404">No task has this identifier.</response>
    /// <response code="412">The task was modified since the ETag sent in If-Match.</response>
    [HttpPut("{id:guid}")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status412PreconditionFailed)]
    public async Task<ActionResult<TaskResponse>> Update(
        Guid id,
        [FromBody] UpdateTaskRequest request,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        CancellationToken cancellationToken)
    {
        var updated = await _taskService.UpdateAsync(id, request, TaskETag.ExpectedVersion(ifMatch), cancellationToken);
        SetETag(updated);
        return Ok(updated);
    }

    /// <summary>
    /// Deletes a task.
    /// </summary>
    /// <param name="id">Task identifier.</param>
    /// <param name="ifMatch">Optional ETag previously returned for this task (e.g. "1").</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <response code="204">The task was deleted.</response>
    /// <response code="404">No task has this identifier.</response>
    /// <response code="412">The task was modified since the ETag sent in If-Match.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status412PreconditionFailed)]
    public async Task<IActionResult> Delete(
        Guid id,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        CancellationToken cancellationToken)
    {
        await _taskService.DeleteAsync(id, TaskETag.ExpectedVersion(ifMatch), cancellationToken);
        return NoContent();
    }

    private void SetETag(TaskResponse task) => Response.Headers.ETag = TaskETag.From(task.Version);
}
