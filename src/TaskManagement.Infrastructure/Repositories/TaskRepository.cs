using Microsoft.EntityFrameworkCore;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;
using TaskManagement.Domain.Interfaces;
using TaskManagement.Infrastructure.Data;

namespace TaskManagement.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of the task repositories. Reads are not tracked; writes are saved immediately.
/// </summary>
public sealed class TaskRepository : ITaskReadRepository, ITaskWriteRepository
{
    private readonly TaskDbContext _context;

    public TaskRepository(TaskDbContext context)
    {
        _context = context;
    }

    public Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Tasks
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public async Task<IReadOnlyList<TaskItem>> ListAsync(
        TaskItemStatus? status,
        DateOnly? dueDate,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Tasks.AsNoTracking();

        if (status.HasValue)
        {
            query = query.Where(t => t.Status == status.Value);
        }

        if (dueDate.HasValue)
        {
            query = query.Where(t => t.DueDate == dueDate.Value);
        }

        return await Order(query).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TaskItem>> SearchAsync(string term, CancellationToken cancellationToken = default)
    {
        var loweredTerm = term.ToLower();

        var query = _context.Tasks
            .AsNoTracking()
            .Where(t => t.Title.ToLower().Contains(loweredTerm) ||
                        (t.Description != null && t.Description.ToLower().Contains(loweredTerm)));

        return await Order(query).ToListAsync(cancellationToken);
    }

    public Task<TaskItem?> FindForUpdateAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Tasks.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public async Task AddAsync(TaskItem task, CancellationToken cancellationToken = default)
    {
        await _context.Tasks.AddAsync(task, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(TaskItem task, CancellationToken cancellationToken = default)
    {
        _context.Tasks.Update(task);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveAsync(TaskItem task, CancellationToken cancellationToken = default)
    {
        _context.Tasks.Remove(task);
        await _context.SaveChangesAsync(cancellationToken);
    }

    // Tasks without a due date go last; ties are broken by creation time.
    private static IOrderedQueryable<TaskItem> Order(IQueryable<TaskItem> query) =>
        query
            .OrderBy(t => t.DueDate == null)
            .ThenBy(t => t.DueDate)
            .ThenBy(t => t.CreatedAt);
}
