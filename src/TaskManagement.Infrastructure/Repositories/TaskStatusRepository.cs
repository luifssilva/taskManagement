using Microsoft.EntityFrameworkCore;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Interfaces;
using TaskManagement.Infrastructure.Data;

namespace TaskManagement.Infrastructure.Repositories;

public sealed class TaskStatusRepository : ITaskStatusReadRepository
{
    private readonly TaskDbContext _context;

    public TaskStatusRepository(TaskDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<TaskItemStatusDefinition>> ListAsync(CancellationToken cancellationToken = default) =>
        await _context.TaskStatuses
            .AsNoTracking()
            .OrderBy(s => s.Id)
            .ToListAsync(cancellationToken);
}
