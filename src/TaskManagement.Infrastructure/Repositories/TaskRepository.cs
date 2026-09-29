using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Exceptions;
using TaskManagement.Domain.Interfaces;
using TaskManagement.Domain.Queries;
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
        TaskListCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Tasks.AsNoTracking();

        if (criteria.Status.HasValue)
        {
            query = query.Where(t => t.Status == criteria.Status.Value);
        }

        if (criteria.DueDate.HasValue)
        {
            query = query.Where(t => t.DueDate == criteria.DueDate.Value);
        }

        if (criteria.Overdue == true)
        {
            query = query.Where(TaskItem.OverdueAsOf(criteria.Today));
        }
        else if (criteria.Overdue == false)
        {
            query = query.Where(Not(TaskItem.OverdueAsOf(criteria.Today)));
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
        await SaveChangesAsync(task, cancellationToken);
    }

    // The task is tracked (loaded by FindForUpdateAsync), so change detection picks up the
    // modified fields and the new history entries.
    public Task UpdateAsync(TaskItem task, CancellationToken cancellationToken = default) =>
        SaveChangesAsync(task, cancellationToken);

    public async Task RemoveAsync(TaskItem task, CancellationToken cancellationToken = default)
    {
        _context.Tasks.Remove(task);
        await SaveChangesAsync(task, cancellationToken);
    }

    private async Task SaveChangesAsync(TaskItem task, CancellationToken cancellationToken)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrencyConflictException(task.Id);
        }
    }

    // Tasks without a due date go last; ties are broken by creation time.
    private static IOrderedQueryable<TaskItem> Order(IQueryable<TaskItem> query) =>
        query
            .OrderBy(t => t.DueDate == null)
            .ThenBy(t => t.DueDate)
            .ThenBy(t => t.CreatedAt);

    private static Expression<Func<TaskItem, bool>> Not(
        Expression<Func<TaskItem, bool>> predicate) =>
        Expression.Lambda<Func<TaskItem, bool>>(
            Expression.Not(predicate.Body),
            predicate.Parameters);
}
