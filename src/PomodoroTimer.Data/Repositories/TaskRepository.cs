using Microsoft.EntityFrameworkCore;
using PomodoroTimer.Data.Context;
using PomodoroTimer.Domain.Entities;
using PomodoroTimer.Domain.Interfaces;

namespace PomodoroTimer.Data.Repositories;

public class TaskRepository : ITaskRepository
{
    private readonly PomodoroDbContext _context;

    public TaskRepository(PomodoroDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<TaskItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.TaskItems
            .Include(t => t.Sessions)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<TaskItem>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.TaskItems
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<TaskItem>> GetByUserIdAsync(int userId, bool? isCompleted = null, CancellationToken cancellationToken = default)
    {
        var query = _context.TaskItems
            .AsNoTracking()
            .Where(t => t.UserId == userId);

        if (isCompleted.HasValue)
        {
            query = query.Where(t => t.IsCompleted == isCompleted.Value);
        }

        return await query
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<TaskItem> AddAsync(TaskItem entity, CancellationToken cancellationToken = default)
    {
        _context.TaskItems.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task UpdateAsync(TaskItem entity, CancellationToken cancellationToken = default)
    {
        if (_context.Entry(entity).State == EntityState.Detached)
        {
            _context.TaskItems.Update(entity);
        }
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(TaskItem entity, CancellationToken cancellationToken = default)
    {
        _context.TaskItems.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
