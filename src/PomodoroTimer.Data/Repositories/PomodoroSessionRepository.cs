using Microsoft.EntityFrameworkCore;
using PomodoroTimer.Data.Context;
using PomodoroTimer.Domain.Entities;
using PomodoroTimer.Domain.Interfaces;

namespace PomodoroTimer.Data.Repositories;

public class PomodoroSessionRepository : IPomodoroSessionRepository
{
    private readonly PomodoroDbContext _context;

    public PomodoroSessionRepository(PomodoroDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<PomodoroSession?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.PomodoroSessions
            .Include(s => s.Task)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<PomodoroSession>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.PomodoroSessions
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<PomodoroSession>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _context.PomodoroSessions
            .AsNoTracking()
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.StartTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<PomodoroSession>> GetByTaskIdAsync(int taskId, CancellationToken cancellationToken = default)
    {
        return await _context.PomodoroSessions
            .AsNoTracking()
            .Where(s => s.TaskId == taskId)
            .OrderByDescending(s => s.StartTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<PomodoroSession> AddAsync(PomodoroSession entity, CancellationToken cancellationToken = default)
    {
        _context.PomodoroSessions.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task UpdateAsync(PomodoroSession entity, CancellationToken cancellationToken = default)
    {
        if (_context.Entry(entity).State == EntityState.Detached)
        {
            _context.PomodoroSessions.Update(entity);
        }
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(PomodoroSession entity, CancellationToken cancellationToken = default)
    {
        _context.PomodoroSessions.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
