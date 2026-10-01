using Microsoft.AspNetCore.Mvc;
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
    /// <param name="id">Task identifier.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <response code="200">The task.</response>
    /// <response code="404">No task has this identifier.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _taskService.GetByIdAsync(id, cancellationToken));
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
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>
    /// Replaces the editable data of a task (title, description, due date and status).
    /// </summary>
    /// <remarks>
    ///     PUT /api/tasks/3fa85f64-5717-4562-b3fc-2c963f66afa6
    ///     {
    ///       "title": "Conferir carga do pedido 4521",
    ///       "description": "Volumes conferidos; aguardando coleta da transportadora",
    ///       "dueDate": "2026-10-02",
    ///       "status": "EmProgresso"
    ///     }
    /// </remarks>
    /// <param name="id">Task identifier.</param>
    /// <param name="request">New task data.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <response code="200">The updated task.</response>
    /// <response code="400">The request contains invalid data.</response>
    /// <response code="404">No task has this identifier.</response>
    [HttpPut("{id:guid}")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskResponse>> Update(
        Guid id,
        [FromBody] UpdateTaskRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _taskService.UpdateAsync(id, request, cancellationToken));
    }

    /// <summary>
    /// Deletes a task.
    /// </summary>
    /// <param name="id">Task identifier.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <response code="204">The task was deleted.</response>
    /// <response code="404">No task has this identifier.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _taskService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
