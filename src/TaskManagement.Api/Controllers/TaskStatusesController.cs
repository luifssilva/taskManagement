using Microsoft.AspNetCore.Mvc;
using TaskManagement.Application.DTOs;
using TaskManagement.Application.Interfaces;

namespace TaskManagement.Api.Controllers;

/// <summary>
/// Exposes the status domain table.
/// </summary>
[ApiController]
[Route("api/task-statuses")]
[Produces("application/json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
public sealed class TaskStatusesController : ControllerBase
{
    private readonly ITaskStatusService _taskStatusService;

    public TaskStatusesController(ITaskStatusService taskStatusService)
    {
        _taskStatusService = taskStatusService;
    }

    /// <summary>
    /// Lists the task statuses: numeric id, name used by the API and description.
    /// </summary>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <response code="200">All task statuses, ordered by id.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<TaskStatusResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TaskStatusResponse>>> List(CancellationToken cancellationToken)
    {
        return Ok(await _taskStatusService.ListAsync(cancellationToken));
    }
}
